using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;
using Sincera.Policies.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Sincera.Policies.Domain.UnitTests.Policies;

public class PremiumCalculatorTests
{
    private static readonly PremiumOptions DefaultOptions = new();

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(1.5, 2)]
    [InlineData(2.5, 2)]
    [InlineData(3.5, 4)]
    [InlineData(123.49, 123)]
    [InlineData(123.50, 124)]
    [InlineData(124.50, 124)]
    public void Rounds_with_bankers_rounding_to_whole_dollars(double input, double expected)
    {
        Assert.Equal((decimal)expected, PremiumCalculator.RoundPremium((decimal)input));
    }

    [Fact]
    public void Floors_at_minimum_annual_premium_when_inputs_are_tiny()
    {
        var calculator = new PremiumCalculator(DefaultOptions);
        var customer = MidAgeCustomerWithCleanHistory();
        var coverages = new List<Coverage> { new(CoverageType.Liability, 100m, 0m) };

        var premium = calculator.Calculate(customer, coverages, new FixedClock(new DateOnly(2026, 5, 20)));

        Assert.Equal(DefaultOptions.MinAnnualPremium, premium);
    }

    [Fact]
    public void Applies_young_driver_multiplier_to_under_25()
    {
        var calculator = new PremiumCalculator(DefaultOptions);
        var today = new DateOnly(2026, 5, 20);
        var young = new Customer(
            new CustomerId("C-1"), "Young", today.AddYears(-22),
            new DrivingHistory(YearsLicensed: 4, AtFaultIncidentsLast5Years: 0),
            "Z");
        var older = new Customer(
            new CustomerId("C-2"), "Older", today.AddYears(-35),
            new DrivingHistory(YearsLicensed: 4, AtFaultIncidentsLast5Years: 0),
            "Z");
        var coverages = new List<Coverage>
        {
            new(CoverageType.Liability, 100_000m, 500m)
        };

        var youngPremium = calculator.Calculate(young, coverages, new FixedClock(today));
        var olderPremium = calculator.Calculate(older, coverages, new FixedClock(today));

        Assert.True(youngPremium > olderPremium,
            $"expected young driver premium ({youngPremium}) to exceed older driver premium ({olderPremium})");
    }

    [Fact]
    public void Applies_loyalty_discount_for_tenure_with_no_at_fault_incidents()
    {
        var calculator = new PremiumCalculator(DefaultOptions);
        var today = new DateOnly(2026, 5, 20);
        var loyal = new Customer(
            new CustomerId("C-1"), "Loyal", today.AddYears(-40),
            new DrivingHistory(YearsLicensed: 15, AtFaultIncidentsLast5Years: 0),
            "Z");
        var newcomer = new Customer(
            new CustomerId("C-2"), "Newcomer", today.AddYears(-40),
            new DrivingHistory(YearsLicensed: 5, AtFaultIncidentsLast5Years: 0),
            "Z");
        var coverages = new List<Coverage>
        {
            new(CoverageType.Liability, 100_000m, 500m)
        };

        var loyalPremium = calculator.Calculate(loyal, coverages, new FixedClock(today));
        var newcomerPremium = calculator.Calculate(newcomer, coverages, new FixedClock(today));

        Assert.True(loyalPremium < newcomerPremium,
            $"expected loyal premium ({loyalPremium}) to be less than newcomer premium ({newcomerPremium})");
    }

    [Fact]
    public void Loyalty_discount_does_not_apply_when_at_fault_incidents_exist()
    {
        var calculator = new PremiumCalculator(DefaultOptions);
        var today = new DateOnly(2026, 5, 20);
        var loyalButCrashes = new Customer(
            new CustomerId("C-1"), "Loyal", today.AddYears(-40),
            new DrivingHistory(YearsLicensed: 15, AtFaultIncidentsLast5Years: 1),
            "Z");
        var loyalClean = new Customer(
            new CustomerId("C-2"), "Loyal", today.AddYears(-40),
            new DrivingHistory(YearsLicensed: 15, AtFaultIncidentsLast5Years: 0),
            "Z");
        var coverages = new List<Coverage>
        {
            new(CoverageType.Liability, 100_000m, 500m)
        };

        var crashPremium = calculator.Calculate(loyalButCrashes, coverages, new FixedClock(today));
        var cleanPremium = calculator.Calculate(loyalClean, coverages, new FixedClock(today));

        Assert.True(crashPremium > cleanPremium,
            $"expected at-fault premium ({crashPremium}) to exceed clean-history premium ({cleanPremium})");
    }

    [Fact]
    public void Applies_high_deductible_credit_per_qualifying_coverage()
    {
        var calculator = new PremiumCalculator(DefaultOptions);
        var today = new DateOnly(2026, 5, 20);
        var customer = MidAgeCustomerWithCleanHistory();
        var highDeductible = new List<Coverage>
        {
            new(CoverageType.Liability, 50_000m, 1_000m),
            new(CoverageType.Collision, 30_000m, 1_000m)
        };
        var lowDeductible = new List<Coverage>
        {
            new(CoverageType.Liability, 50_000m, 500m),
            new(CoverageType.Collision, 30_000m, 500m)
        };

        var highPremium = calculator.Calculate(customer, highDeductible, new FixedClock(today));
        var lowPremium = calculator.Calculate(customer, lowDeductible, new FixedClock(today));

        Assert.True(highPremium < lowPremium,
            $"expected high-deductible premium ({highPremium}) to be less than low-deductible premium ({lowPremium})");
    }

    [Fact]
    public void At_fault_loading_caps_at_configured_incident_count()
    {
        var calculator = new PremiumCalculator(DefaultOptions);
        var today = new DateOnly(2026, 5, 20);
        var three = new Customer(
            new CustomerId("C-1"), "Three", today.AddYears(-40),
            new DrivingHistory(YearsLicensed: 5, AtFaultIncidentsLast5Years: 3),
            "Z");
        var five = new Customer(
            new CustomerId("C-2"), "Five", today.AddYears(-40),
            new DrivingHistory(YearsLicensed: 5, AtFaultIncidentsLast5Years: 5),
            "Z");
        var coverages = new List<Coverage>
        {
            new(CoverageType.Liability, 100_000m, 500m)
        };

        var threePremium = calculator.Calculate(three, coverages, new FixedClock(today));
        var fivePremium = calculator.Calculate(five, coverages, new FixedClock(today));

        Assert.Equal(fivePremium, threePremium);
    }

    private static Customer MidAgeCustomerWithCleanHistory() =>
        new(new CustomerId("C-mid"), "Mid", new DateOnly(1985, 1, 1),
            new DrivingHistory(YearsLicensed: 8, AtFaultIncidentsLast5Years: 0),
            "Z");
}
