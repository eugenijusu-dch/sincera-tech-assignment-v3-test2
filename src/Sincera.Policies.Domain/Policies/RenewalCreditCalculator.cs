using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Common;

namespace Sincera.Policies.Domain.Policies;

public sealed class RenewalCreditCalculator
{
    private const int RenewalsRequiredForFeeWaiver = 2;
    private const int ClaimFreeMonthsRequiredForFeeWaiver = 12;
    private const decimal TenureCreditPerRenewal = 25m;
    private const int TenureCreditCap = 5;
    private const decimal ClaimFreeCredit = 100m;
    private const decimal CreditPercentageCap = 0.20m;
    private const decimal MinimumPremiumForCredit = 250m;

    public decimal ComputeRenewalCredit(Policy oldPolicy, decimal newAnnualPremium, IReadOnlyList<Claim> claims)
    {
        if (oldPolicy is null) throw new ArgumentNullException(nameof(oldPolicy));
        if (claims is null) throw new ArgumentNullException(nameof(claims));
        if (newAnnualPremium < MinimumPremiumForCredit) return 0m;

        var tenureCredit = Math.Min(oldPolicy.RenewalCount, TenureCreditCap) * TenureCreditPerRenewal;

        var claimsInPriorTerm = claims.Count(c =>
            c.PolicyId.Equals(oldPolicy.Id)
            && IsWithinPolicyTerm(oldPolicy, c.IncidentDate));

        var claimFreeBonus = claimsInPriorTerm == 0 ? ClaimFreeCredit : 0m;

        var totalCredit = tenureCredit + claimFreeBonus;

        var cap = newAnnualPremium * CreditPercentageCap;
        if (totalCredit > cap) totalCredit = cap;

        return PremiumCalculator.RoundPremium(totalCredit);
    }

    public RefundEligibility ComputeRefundEligibility(Policy policy, IReadOnlyList<Claim> claims, IClock clock)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        if (claims is null) throw new ArgumentNullException(nameof(claims));
        if (clock is null) throw new ArgumentNullException(nameof(clock));

        if (policy.RenewalCount < RenewalsRequiredForFeeWaiver)
            return RefundEligibility.Ineligible($"Requires at least {RenewalsRequiredForFeeWaiver} renewals.");

        var cutoff = clock.Today.AddMonths(-ClaimFreeMonthsRequiredForFeeWaiver);
        var hasRecentClaim = claims.Any(c =>
            c.PolicyId.Equals(policy.Id)
            && c.IncidentDate >= cutoff);

        if (hasRecentClaim)
            return RefundEligibility.Ineligible($"Has a claim within the last {ClaimFreeMonthsRequiredForFeeWaiver} months.");

        return RefundEligibility.EligibleForFeeWaiver(
            $"Customer has {policy.RenewalCount} renewals and is claim-free for {ClaimFreeMonthsRequiredForFeeWaiver}+ months.");
    }

    public static bool IsWithinPolicyTerm(Policy policy, DateOnly date)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        if (policy.EffectiveDate is null || policy.ExpiryDate is null) return false;
        return date >= policy.EffectiveDate.Value && date <= policy.ExpiryDate.Value;
    }
}
