using Sincera.Policies.Domain.Exceptions;

namespace Sincera.Policies.Domain.Policies;

public sealed class CancellationPolicy
{
    public decimal ComputeProratedRefund(
        Policy policy,
        DateOnly cancellationDate,
        CancellationOptions options,
        RefundEligibility eligibility)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (eligibility is null) throw new ArgumentNullException(nameof(eligibility));

        if (policy.Status != PolicyStatus.Active)
            throw new DomainException("policy.not_active", "Only active policies can be cancelled with a refund.");

        if (policy.EffectiveDate is null || policy.ExpiryDate is null || policy.AnnualPremium is null)
            throw new DomainException("policy.missing_dates", "Active policy is missing effective/expiry dates or premium.");

        var effective = policy.EffectiveDate.Value;
        var expiry = policy.ExpiryDate.Value;

        if (cancellationDate < effective) cancellationDate = effective;
        if (cancellationDate > expiry) cancellationDate = expiry;

        var totalDays = expiry.DayNumber - effective.DayNumber;
        if (totalDays <= 0) return 0m;

        var unusedDays = expiry.DayNumber - cancellationDate.DayNumber;
        if (unusedDays <= 0) return 0m;

        var prorated = policy.AnnualPremium.Value * unusedDays / totalDays;

        if (!eligibility.WaiveAdminFee)
            prorated -= options.AdminFee;

        if (prorated < 0) prorated = 0;

        return PremiumCalculator.RoundPremium(prorated);
    }
}
