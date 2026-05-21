using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Domain.UnitTests.Policies;

public class CancellationPolicyTests
{
    private static readonly CancellationOptions Options = new() { AdminFee = 35m, MinNoticeDays = 0 };
    private static readonly CustomerId CustomerId = new("C-T");

    [Theory]
    [InlineData(0, false, 330)]
    [InlineData(0, true, 365)]
    [InlineData(100, true, 265)]
    [InlineData(100, false, 230)]
    [InlineData(365, false, 0)]
    [InlineData(365, true, 0)]
    public void Refund_is_prorated_by_remaining_days(int daysIn, bool waiveAdminFee, decimal expectedRefund)
    {
        var policy = ActivePolicy(annualPremium: 365m, daysIn: 0, termDays: 365);
        var policyService = new CancellationPolicy();
        var eligibility = new RefundEligibility(WaiveAdminFee: waiveAdminFee, Reason: "test");
        var cancellationDate = policy.EffectiveDate!.Value.AddDays(daysIn);

        var refund = policyService.ComputeProratedRefund(policy, cancellationDate, Options, eligibility);

        Assert.Equal(expectedRefund, refund);
    }

    [Fact]
    public void Refund_waives_admin_fee_for_eligible_customer()
    {
        var policy = ActivePolicy(annualPremium: 365m, daysIn: 0, termDays: 365);
        var policyService = new CancellationPolicy();
        var waived = new RefundEligibility(WaiveAdminFee: true, "loyalty");
        var charged = new RefundEligibility(WaiveAdminFee: false, "newer customer");

        var waivedRefund = policyService.ComputeProratedRefund(policy, policy.EffectiveDate!.Value, Options, waived);
        var chargedRefund = policyService.ComputeProratedRefund(policy, policy.EffectiveDate!.Value, Options, charged);

        Assert.Equal(Options.AdminFee, waivedRefund - chargedRefund);
    }

    [Fact]
    public void Refund_for_non_active_policy_throws()
    {
        var policy = Policy.Rehydrate(
            id: new PolicyId("P-X"), customerId: CustomerId, status: PolicyStatus.Cancelled,
            effectiveDate: new DateOnly(2026, 1, 1), expiryDate: new DateOnly(2026, 12, 31),
            annualPremium: 365m, cancellationRefund: 100m, cancelledAtUtc: DateTime.UtcNow,
            cancellationReason: "test", renewalCount: 0, coverages: []);
        var policyService = new CancellationPolicy();
        var eligibility = new RefundEligibility(false, "test");

        var ex = Assert.Throws<DomainException>(
            () => policyService.ComputeProratedRefund(policy, new DateOnly(2026, 6, 1), Options, eligibility));

        Assert.Equal("policy.not_active", ex.Code);
    }

    private static Policy ActivePolicy(decimal annualPremium, int daysIn, int termDays)
    {
        var effective = new DateOnly(2026, 1, 1);
        return Policy.Rehydrate(
            id: new PolicyId("P-T"),
            customerId: CustomerId,
            status: PolicyStatus.Active,
            effectiveDate: effective,
            expiryDate: effective.AddDays(termDays),
            annualPremium: annualPremium,
            cancellationRefund: null,
            cancelledAtUtc: null,
            cancellationReason: null,
            renewalCount: 0,
            coverages: []);
    }
}
