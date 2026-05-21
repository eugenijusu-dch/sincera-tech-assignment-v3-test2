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
    private static readonly DateOnly Today = new(2026, 5, 20);

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public FileClaimEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_claims_returns_201_with_claim()
    {
        var policy = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _factory.Claims.Setup(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new { policyId = policy.Id.Value, incidentDate = "2026-05-15", claimedAmount = 1000, description = "Rear-end collision" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FileClaimResponse>();
        Assert.NotNull(body);
        Assert.Equal(policy.Id.Value, body!.PolicyId);
        Assert.False(body.RequiresInspection);
        _factory.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task POST_claims_sets_RequiresInspection_for_high_amount()
    {
        var policy = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _factory.Claims.Setup(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new { policyId = policy.Id.Value, incidentDate = "2026-05-15", claimedAmount = 10000, description = "Total loss" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FileClaimResponse>();
        Assert.True(body!.RequiresInspection);
    }

    [Fact]
    public async Task POST_claims_returns_409_for_non_active_policy()
    {
        var policy = new Policy(new PolicyId("P-draft"), new CustomerId("C-1"));
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var request = new { policyId = "P-draft", incidentDate = "2026-05-15", claimedAmount = 500, description = "desc" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task POST_claims_returns_404_for_missing_policy()
    {
        _factory.Policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var request = new { policyId = "missing", incidentDate = "2026-05-15", claimedAmount = 500, description = "desc" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_claims_returns_400_for_negative_amount()
    {
        var request = new { policyId = "P-1", incidentDate = "2026-05-15", claimedAmount = -1, description = "desc" };
        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
