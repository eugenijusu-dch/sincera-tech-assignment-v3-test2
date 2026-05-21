using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Api.IntegrationTests.Customers;

public class ListCustomerPoliciesEndpointTests : IClassFixture<ApiFactory>
{
    private static readonly DateOnly Today = new(2026, 5, 20);
    private static readonly CustomerId CustomerId = new("C-1");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ListCustomerPoliciesEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_returns_200_with_paged_policies()
    {
        var active = MakePolicy("P-1", PolicyStatus.Active, Today, Today.AddYears(1));
        _factory.Policies
            .Setup(r => r.GetByCustomerIdAsync(CustomerId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { active }, 1));

        var response = await _client.GetAsync("/api/customers/C-1/policies?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListCustomerPoliciesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(1, body!.TotalCount);
        Assert.Single(body.Items);
        Assert.Equal("P-1", body.Items[0].Id);
    }

    [Fact]
    public async Task GET_passes_status_filter()
    {
        var active = MakePolicy("P-1", PolicyStatus.Active, Today, Today.AddYears(1));
        _factory.Policies
            .Setup(r => r.GetByCustomerIdAsync(CustomerId, PolicyStatus.Active, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { active }, 1));

        var response = await _client.GetAsync("/api/customers/C-1/policies?status=Active&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListCustomerPoliciesResponse>(JsonOptions);
        Assert.Equal(1, body!.TotalCount);
    }

    [Fact]
    public async Task GET_returns_empty_list_when_no_policies()
    {
        _factory.Policies
            .Setup(r => r.GetByCustomerIdAsync(CustomerId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Policy>(), 0));

        var response = await _client.GetAsync("/api/customers/C-1/policies?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListCustomerPoliciesResponse>(JsonOptions);
        Assert.Equal(0, body!.TotalCount);
        Assert.Empty(body.Items);
    }

    private static Policy MakePolicy(string id, PolicyStatus status, DateOnly effective, DateOnly expiry) =>
        Policy.Rehydrate(
            new PolicyId(id), CustomerId, status, effective, expiry,
            342m, null, null, null, 0,
            new[] { new Coverage(CoverageType.Liability, 30_000m, 500m) });
}
