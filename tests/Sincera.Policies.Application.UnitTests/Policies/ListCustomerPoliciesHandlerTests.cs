using Moq;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Mapping;
using Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;
using Sincera.Policies.Application.UnitTests.TestDoubles;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Application.UnitTests.Policies;

public class ListCustomerPoliciesHandlerTests
{
    private static readonly DateOnly Today = new(2026, 5, 20);
    private static readonly CustomerId CustomerId = new("C-1");

    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly FakeClock _clock = new(Today);
    private readonly PolicyMapper _mapper = new();

    [Fact]
    public async Task Returns_paged_policies_for_customer()
    {
        var active = MakePolicy("P-1", PolicyStatus.Active, Today, Today.AddYears(1));
        var expired = MakePolicy("P-2", PolicyStatus.Expired, Today.AddYears(-1), Today.AddDays(-1));
        _policies.Setup(r => r.GetByCustomerIdAsync(CustomerId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { active, expired }, 2));

        var handler = new ListCustomerPoliciesQueryHandler(_policies.Object, _mapper);
        var result = await handler.Handle(
            new ListCustomerPoliciesQuery(CustomerId, null, 1, 20), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("P-1", result.Items[0].Id);
    }

    [Fact]
    public async Task Passes_status_filter_to_repository()
    {
        var active = MakePolicy("P-1", PolicyStatus.Active, Today, Today.AddYears(1));
        _policies.Setup(r => r.GetByCustomerIdAsync(CustomerId, PolicyStatus.Active, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { active }, 1));

        var handler = new ListCustomerPoliciesQueryHandler(_policies.Object, _mapper);
        var result = await handler.Handle(
            new ListCustomerPoliciesQuery(CustomerId, PolicyStatus.Active, 1, 10), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(PolicyStatus.Active, result.Items[0].Status);
    }

    [Fact]
    public async Task Returns_empty_page_when_no_policies()
    {
        _policies.Setup(r => r.GetByCustomerIdAsync(CustomerId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Policy>(), 0));

        var handler = new ListCustomerPoliciesQueryHandler(_policies.Object, _mapper);
        var result = await handler.Handle(
            new ListCustomerPoliciesQuery(CustomerId, null, 1, 20), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    private static Policy MakePolicy(string id, PolicyStatus status, DateOnly effective, DateOnly expiry) =>
        Policy.Rehydrate(
            new PolicyId(id), CustomerId, status, effective, expiry,
            342m, null, null, null, 0,
            new[] { new Coverage(CoverageType.Liability, 30_000m, 500m) });
}
