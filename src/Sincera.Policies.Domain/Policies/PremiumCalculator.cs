using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Customers;

namespace Sincera.Policies.Domain.Policies;

public sealed class PremiumCalculator
{
    private readonly PremiumOptions _options;

    public PremiumCalculator(PremiumOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public decimal Calculate(Customer customer, IReadOnlyList<Coverage> coverages, IClock clock)
    {
        if (customer is null) throw new ArgumentNullException(nameof(customer));
        if (coverages is null) throw new ArgumentNullException(nameof(coverages));
        if (clock is null) throw new ArgumentNullException(nameof(clock));
        if (coverages.Count == 0) return RoundPremium(_options.MinAnnualPremium);

        var basePremium = coverages.Sum(c => c.LimitAmount * _options.RateFor(c.Type));

        var loyaltyEligible =
            customer.DrivingHistory.YearsLicensed >= _options.LoyaltyYearsLicensedThreshold
            && customer.DrivingHistory.AtFaultIncidentsLast5Years == 0;

        if (loyaltyEligible)
            basePremium *= 1m - _options.LoyaltyDiscount;

        var age = customer.AgeOn(clock.Today);
        var ageMultiplier = age < _options.YoungDriverAgeCutoff
            ? _options.YoungDriverMultiplier
            : age < _options.MidAgeDriverCutoff
                ? _options.MidAgeDriverMultiplier
                : 1.0m;

        var atFaultLoading = 1m + (_options.AtFaultLoadingPerIncident
            * Math.Min(customer.DrivingHistory.AtFaultIncidentsLast5Years, _options.AtFaultIncidentsCap));

        var premium = basePremium * ageMultiplier * atFaultLoading;

        var highDeductibleCount = coverages.Count(c => c.Deductible >= _options.HighDeductibleThreshold);
        premium -= highDeductibleCount * _options.HighDeductibleCreditPerCoverage;

        if (premium < _options.MinAnnualPremium)
            premium = _options.MinAnnualPremium;

        return RoundPremium(premium);
    }

    public static decimal RoundPremium(decimal value)
        => Math.Round(value, 0, MidpointRounding.ToEven);
}
