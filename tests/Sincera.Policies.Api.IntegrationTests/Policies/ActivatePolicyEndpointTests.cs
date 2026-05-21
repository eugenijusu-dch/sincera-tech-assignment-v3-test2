using System.Net;
using System.Net.Http.Json;
using Moq;
using Sincera.Policies.Application.Policies.Commands.ActivatePolicy;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Api.IntegrationTests.Policies;

public class ActivatePolicyEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ActivatePolicyEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_activate_returns_200_with_response()
    {
        var customer = new Customer(
            new CustomerId("C-1"), "Test Customer", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var policy = new Policy(new PolicyId("P-1"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _factory.Customers.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var response = await _client.PostAsync("/api/policies/P-1/activate", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ActivatePolicyResponse>();
        Assert.NotNull(body);
        Assert.Equal("P-1", body!.PolicyId);
        Assert.True(body.AnnualPremium > 0);
        _factory.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task POST_activate_returns_404_when_policy_not_found()
    {
        _factory.Policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var response = await _client.PostAsync("/api/policies/missing/activate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_activate_returns_409_when_already_active()
    {
        var customer = new Customer(
            new CustomerId("C-1"), "Test Customer", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var policy = new Policy(new PolicyId("P-active"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), _factory.Clock);
        _factory.Policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _factory.Customers.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var response = await _client.PostAsync("/api/policies/P-active/activate", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GET_returns_404_when_policy_not_found()
    {
        _factory.Policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var response = await _client.GetAsync("/api/policies/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
