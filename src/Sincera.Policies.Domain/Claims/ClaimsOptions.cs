namespace Sincera.Policies.Domain.Claims;

public sealed class ClaimsOptions
{
    public decimal InspectionThreshold { get; set; } = 5000m;
    public int FreshPolicyInspectionDays { get; set; } = 30;
}
