namespace Sincera.Policies.Domain.Policies;

public sealed class PremiumOptions
{
    public decimal LiabilityRate { get; set; } = 0.012m;
    public decimal CollisionRate { get; set; } = 0.025m;
    public decimal ComprehensiveRate { get; set; } = 0.018m;
    public decimal UninsuredMotoristRate { get; set; } = 0.008m;

    public decimal MinAnnualPremium { get; set; } = 250m;

    public int YoungDriverAgeCutoff { get; set; } = 25;
    public int MidAgeDriverCutoff { get; set; } = 30;
    public decimal YoungDriverMultiplier { get; set; } = 1.40m;
    public decimal MidAgeDriverMultiplier { get; set; } = 1.15m;

    public int AtFaultIncidentsCap { get; set; } = 3;
    public decimal AtFaultLoadingPerIncident { get; set; } = 0.15m;

    public int LoyaltyYearsLicensedThreshold { get; set; } = 10;
    public decimal LoyaltyDiscount { get; set; } = 0.05m;

    public decimal HighDeductibleThreshold { get; set; } = 1000m;
    public decimal HighDeductibleCreditPerCoverage { get; set; } = 50m;

    public decimal RateFor(CoverageType type) => type switch
    {
        CoverageType.Liability => LiabilityRate,
        CoverageType.Collision => CollisionRate,
        CoverageType.Comprehensive => ComprehensiveRate,
        CoverageType.UninsuredMotorist => UninsuredMotoristRate,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown coverage type.")
    };
}
