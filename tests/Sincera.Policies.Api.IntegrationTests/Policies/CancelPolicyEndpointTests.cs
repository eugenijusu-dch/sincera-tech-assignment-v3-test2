using System.Net;
using System.Net.Http.Json;
using Moq;
using Sincera.Policies.Application.Policies.Commands.CancelPolicy;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Api.IntegrationTests.Policies;

public class CancelPolicyEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public CancelPolicyEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_cancel_returns_200_with_refund()
    {
        var (policy, _) = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _factory.Claims.Setup(r => r.GetByPolicyIdAsync(policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var request = new { effectiveCancellationDate = "2026-06-01", reason = "No longer needed" };
        var response = await _client.PostAsJsonAsync($"/api/policies/{policy.Id.Value}/cancel", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CancelPolicyResponse>();
        Assert.NotNull(body);
        Assert.Equal(policy.Id.Value, body!.PolicyId);
        Assert.True(body.Refund >= 0);
        _factory.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task POST_cancel_returns_409_when_policy_not_active()
    {
        var policy = new Policy(new PolicyId("P-draft"), new CustomerId("C-1"));
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var request = new { effectiveCancellationDate = "2026-06-01", reason = "reason" };
        var response = await _client.PostAsJsonAsync("/api/policies/P-draft/cancel", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task POST_cancel_returns_404_when_policy_not_found()
    {
        _factory.Policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var request = new { effectiveCancellationDate = "2026-06-01", reason = "reason" };
        var response = await _client.PostAsJsonAsync("/api/policies/missing/cancel", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_cancel_already_cancelled_returns_409()
    {
        var (policy, _) = BuildActivePolicy();
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _factory.Claims.Setup(r => r.GetByPolicyIdAsync(policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var request = new { effectiveCancellationDate = "2026-06-01", reason = "first cancel" };
        await _client.PostAsJsonAsync($"/api/policies/{policy.Id.Value}/cancel", request);

        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        var response2 = await _client.PostAsJsonAsync($"/api/policies/{policy.Id.Value}/cancel", request);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    private (Policy policy, Customer customer) BuildActivePolicy()
    {
        var customer = new Customer(
            new CustomerId("C-1"), "Test Customer", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var policy = new Policy(new PolicyId("P-active"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), _factory.Clock);
        return (policy, customer);
    }
}
