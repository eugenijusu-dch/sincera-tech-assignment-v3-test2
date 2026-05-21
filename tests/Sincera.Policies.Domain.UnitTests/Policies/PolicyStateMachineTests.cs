using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;
using Sincera.Policies.Domain.Policies.Events;
using Sincera.Policies.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Sincera.Policies.Domain.UnitTests.Policies;

public class PolicyStateMachineTests
{
    private static readonly DateOnly Today = new(2026, 5, 20);

    [Fact]
    public void Activate_transitions_draft_to_active_and_raises_event()
    {
        var customer = NewCustomer();
        var policy = new Policy(new PolicyId("P-1"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));

        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), new FixedClock(Today));

        Assert.Equal(PolicyStatus.Active, policy.Status);
        Assert.Equal(Today, policy.EffectiveDate);
        Assert.Equal(Today.AddYears(1), policy.ExpiryDate);
        Assert.NotNull(policy.AnnualPremium);
        Assert.Single(policy.DomainEvents, e => e is PolicyActivated);
    }

    [Fact]
    public void Activate_without_coverage_throws()
    {
        var customer = NewCustomer();
        var policy = new Policy(new PolicyId("P-1"), customer.Id);

        var ex = Assert.Throws<DomainException>(
            () => policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), new FixedClock(Today)));

        Assert.Equal("policy.no_coverage", ex.Code);
    }

    [Fact]
    public void Activate_when_already_active_throws_invalid_transition()
    {
        var customer = NewCustomer();
        var policy = NewActivePolicy(customer);

        Assert.Throws<InvalidPolicyTransitionException>(
            () => policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), new FixedClock(Today)));
    }

    [Fact]
    public void Activate_with_wrong_customer_throws()
    {
        var ownCustomer = NewCustomer(id: "C-1");
        var policy = new Policy(new PolicyId("P-1"), ownCustomer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        var otherCustomer = NewCustomer(id: "C-other");

        var ex = Assert.Throws<DomainException>(
            () => policy.Activate(otherCustomer, new PremiumCalculator(new PremiumOptions()), new FixedClock(Today)));

        Assert.Equal("policy.customer_mismatch", ex.Code);
    }

    [Fact]
    public void MarkExpired_succeeds_after_expiry_date()
    {
        var customer = NewCustomer();
        var policy = NewActivePolicy(customer);
        var future = new FixedClock(policy.ExpiryDate!.Value.AddDays(1));

        policy.MarkExpired(future);

        Assert.Equal(PolicyStatus.Expired, policy.Status);
    }

    [Fact]
    public void MarkExpired_before_expiry_throws()
    {
        var customer = NewCustomer();
        var policy = NewActivePolicy(customer);

        var ex = Assert.Throws<DomainException>(() => policy.MarkExpired(new FixedClock(Today)));

        Assert.Equal("policy.not_yet_expired", ex.Code);
    }

    [Fact]
    public void AddCoverage_on_active_policy_throws()
    {
        var customer = NewCustomer();
        var policy = NewActivePolicy(customer);

        var ex = Assert.Throws<DomainException>(
            () => policy.AddCoverage(new Coverage(CoverageType.Collision, 10_000m, 500m)));

        Assert.Equal("policy.coverage_locked", ex.Code);
    }

    private static Customer NewCustomer(string id = "C-1") =>
        new(new CustomerId(id), "Test Customer", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "Z");

    private static Policy NewActivePolicy(Customer customer)
    {
        var policy = new Policy(new PolicyId("P-1"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), new FixedClock(Today));
        return policy;
    }
}
