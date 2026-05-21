using System.Net;
using System.Net.Http.Json;
using Moq;
using Sincera.Policies.Application.Claims.Commands.FileClaim;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Api.IntegrationTests.Claims;

public class FileClaimEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public FileClaimEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_file_returns_201_with_response()
    {
        var policy = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var request = new { policyId = policy.Id.Value, incidentDate = "2026-05-15", claimedAmount = 1500m, description = "fender bender" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FileClaimResponse>();
        Assert.NotNull(body);
        Assert.Equal(policy.Id.Value, body!.PolicyId);
        _factory.Claims.Verify(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()), Times.Once);
        _factory.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task POST_file_returns_409_when_policy_not_active()
    {
        var policy = new Policy(new PolicyId("P-draft"), new CustomerId("C-1"));
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var request = new { policyId = "P-draft", incidentDate = "2026-05-15", claimedAmount = 500m, description = "x" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task POST_file_returns_404_when_policy_not_found()
    {
        _factory.Policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var request = new { policyId = "missing", incidentDate = "2026-05-15", claimedAmount = 500m, description = "x" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_file_returns_400_when_amount_non_positive()
    {
        var request = new { policyId = "P-1", incidentDate = "2026-05-15", claimedAmount = 0m, description = "x" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_file_returns_400_when_description_empty()
    {
        var request = new { policyId = "P-1", incidentDate = "2026-05-15", claimedAmount = 500m, description = "" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_file_returns_400_when_incident_date_future()
    {
        var policy = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var request = new { policyId = policy.Id.Value, incidentDate = "2030-01-01", claimedAmount = 500m, description = "future" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_file_sets_inspection_for_high_amount()
    {
        var policy = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var request = new { policyId = policy.Id.Value, incidentDate = "2026-05-15", claimedAmount = 10_000m, description = "big" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FileClaimResponse>();
        Assert.True(body!.RequiresInspection);
    }

    [Fact]
    public async Task POST_file_sets_inspection_for_fresh_policy_incident()
    {
        var policy = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var incident = policy.EffectiveDate!.Value;
        var request = new { policyId = policy.Id.Value, incidentDate = incident.ToString("yyyy-MM-dd"), claimedAmount = 500m, description = "fresh" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FileClaimResponse>();
        Assert.True(body!.RequiresInspection);
    }

    private Policy BuildActivePolicy()
    {
        var customer = new Customer(
            new CustomerId("C-1"), "Test Customer", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var policy = new Policy(new PolicyId("P-active"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), _factory.Clock);
        return policy;
    }
}
