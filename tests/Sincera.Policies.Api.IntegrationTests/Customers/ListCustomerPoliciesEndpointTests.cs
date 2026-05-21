using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Api.IntegrationTests.Customers;

public class ListCustomerPoliciesEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ListCustomerPoliciesEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GET_returns_paged_list_with_total()
    {
        var customerId = new CustomerId("C-1");
        var items = new List<Policy>
        {
            BuildPolicy("P-A", customerId),
            BuildPolicy("P-B", customerId)
        };
        _factory.Policies.Setup(r => r.ListByCustomerAsync(
                customerId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Policy>)items, 5));

        var response = await _client.GetAsync("/api/customers/C-1/policies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListCustomerPoliciesResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(5, body!.Total);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        Assert.Equal(2, body.Items.Count);
    }

    [Fact]
    public async Task GET_passes_status_and_pagination_to_repo()
    {
        var customerId = new CustomerId("C-1");
        _factory.Policies.Setup(r => r.ListByCustomerAsync(
                customerId, PolicyStatus.Active, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Policy>)[], 0));

        var response = await _client.GetAsync("/api/customers/C-1/policies?status=Active&page=2&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.Policies.Verify(r => r.ListByCustomerAsync(
            customerId, PolicyStatus.Active, 2, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GET_clamps_invalid_pagination_to_defaults()
    {
        var customerId = new CustomerId("C-1");
        _factory.Policies.Setup(r => r.ListByCustomerAsync(
                customerId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Policy>)[], 0));

        var response = await _client.GetAsync("/api/customers/C-1/policies?page=0&pageSize=-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.Policies.Verify(r => r.ListByCustomerAsync(
            customerId, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Policy BuildPolicy(string id, CustomerId customerId) =>
        Policy.Rehydrate(
            id: new PolicyId(id),
            customerId: customerId,
            status: PolicyStatus.Active,
            effectiveDate: new DateOnly(2026, 1, 1),
            expiryDate: new DateOnly(2027, 1, 1),
            annualPremium: 1000m,
            cancellationRefund: null,
            cancelledAtUtc: null,
            cancellationReason: null,
            renewalCount: 0,
            coverages: Array.Empty<Coverage>());
}
