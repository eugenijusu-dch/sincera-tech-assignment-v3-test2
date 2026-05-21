using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Sincera.Policies.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Sincera.Policies.Domain.UnitTests.Policies;

public class RenewalCreditCalculatorTests
{
    private static readonly CustomerId CustomerId = new("C-T");
    private static readonly PolicyId PolicyId = new("P-T");

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 125)]
    [InlineData(3, 175)]
    [InlineData(5, 225)]
    [InlineData(7, 225)]
    [InlineData(20, 225)]
    public void Tenure_credit_capped_at_five_renewals(int renewalCount, decimal expectedCredit)
    {
        var calc = new RenewalCreditCalculator();
        var policy = SeededPolicy(renewalCount: renewalCount);

        var credit = calc.ComputeRenewalCredit(policy, 5_000m, []);

        Assert.Equal(expectedCredit, credit);
    }

    [Fact]
    public void No_credit_when_new_premium_below_minimum()
    {
        var calc = new RenewalCreditCalculator();
        var policy = SeededPolicy(renewalCount: 3);

        var credit = calc.ComputeRenewalCredit(policy, 200m, []);

        Assert.Equal(0m, credit);
    }

    [Fact]
    public void Claim_inside_term_removes_claim_free_credit()
    {
        var calc = new RenewalCreditCalculator();
        var policy = SeededPolicy(renewalCount: 3);
        var claim = new Claim(
            new ClaimId("C-1"), PolicyId, policy.EffectiveDate!.Value.AddDays(50), 1_000m,
            "test", false, new FixedClock(new DateOnly(2026, 5, 20)));

        var credit = calc.ComputeRenewalCredit(policy, 5_000m, [claim]);

        Assert.Equal(3 * 25m, credit);
    }

    [Fact]
    public void ComputeRefundEligibility_waives_fee_for_loyal_claim_free_customer()
    {
        var calc = new RenewalCreditCalculator();
        var policy = SeededPolicy(renewalCount: 3);
        var clock = new FixedClock(new DateOnly(2026, 5, 20));

        var eligibility = calc.ComputeRefundEligibility(policy, [], clock);

        Assert.True(eligibility.WaiveAdminFee);
    }

    [Fact]
    public void ComputeRefundEligibility_denies_when_renewals_insufficient()
    {
        var calc = new RenewalCreditCalculator();
        var policy = SeededPolicy(renewalCount: 1);
        var clock = new FixedClock(new DateOnly(2026, 5, 20));

        var eligibility = calc.ComputeRefundEligibility(policy, [], clock);

        Assert.False(eligibility.WaiveAdminFee);
    }

    [Fact]
    public void ComputeRefundEligibility_denies_when_recent_claim_exists()
    {
        var calc = new RenewalCreditCalculator();
        var policy = SeededPolicy(renewalCount: 3);
        var clock = new FixedClock(new DateOnly(2026, 5, 20));
        var recentClaim = new Claim(
            new ClaimId("C-r"), PolicyId, clock.Today.AddMonths(-3), 1_500m,
            "recent", false, clock);

        var eligibility = calc.ComputeRefundEligibility(policy, [recentClaim], clock);

        Assert.False(eligibility.WaiveAdminFee);
    }

    private static Policy SeededPolicy(int renewalCount)
    {
        var today = new DateOnly(2026, 5, 20);
        return Policy.Rehydrate(
            id: PolicyId,
            customerId: CustomerId,
            status: PolicyStatus.Active,
            effectiveDate: today.AddDays(-180),
            expiryDate: today.AddDays(185),
            annualPremium: 1_000m,
            cancellationRefund: null,
            cancelledAtUtc: null,
            cancellationReason: null,
            renewalCount: renewalCount,
            coverages: []);
    }
}
