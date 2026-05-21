namespace Sincera.Policies.Domain.Policies;

public sealed record RefundEligibility(bool WaiveAdminFee, string Reason)
{
    public static RefundEligibility Ineligible(string reason) => new(false, reason);

    public static RefundEligibility EligibleForFeeWaiver(string reason) => new(true, reason);
}
