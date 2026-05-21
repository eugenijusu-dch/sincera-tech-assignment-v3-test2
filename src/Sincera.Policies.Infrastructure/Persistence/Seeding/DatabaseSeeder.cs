using Microsoft.Extensions.Options;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Seeding;

public static class DatabaseSeeder
{
    public static void Seed(PoliciesDbContext db, IClock clock, IOptions<PremiumOptions> premiumOptions)
    {
        if (db.Customers.Any()) return;

        var today = clock.Today;
        var calculator = new PremiumCalculator(premiumOptions.Value);

        var anna = new Customer(
            new CustomerId("C-9001"),
            "Anna Berzina",
            new DateOnly(1993, 6, 12),
            new DrivingHistory(YearsLicensed: 12, AtFaultIncidentsLast5Years: 0),
            "LV-1050");

        var mikus = new Customer(
            new CustomerId("C-9002"),
            "Mikus Ozols",
            new DateOnly(2003, 2, 8),
            new DrivingHistory(YearsLicensed: 4, AtFaultIncidentsLast5Years: 1),
            "LV-3001");

        var rita = new Customer(
            new CustomerId("C-9003"),
            "Rita Kalnina",
            new DateOnly(1970, 11, 30),
            new DrivingHistory(YearsLicensed: 30, AtFaultIncidentsLast5Years: 2),
            "LV-1050");

        var edgars = new Customer(
            new CustomerId("C-9004"),
            "Edgars Liepa",
            new DateOnly(1985, 3, 22),
            new DrivingHistory(YearsLicensed: 18, AtFaultIncidentsLast5Years: 0),
            "LV-2020");

        db.Customers.AddRange(anna, mikus, rita, edgars);

        var p1001 = SeedActivePolicy(
            id: "P-1001", customer: anna, calculator: calculator, clock: clock,
            effectiveDaysAgo: 90, termDays: 365, renewalCount: 2,
            coverages:
            [
                new Coverage(CoverageType.Liability, 50_000m, 500m),
                new Coverage(CoverageType.Comprehensive, 25_000m, 1_000m)
            ]);

        var p1002 = SeedActivePolicy(
            id: "P-1002", customer: mikus, calculator: calculator, clock: clock,
            effectiveDaysAgo: 15, termDays: 365, renewalCount: 0,
            coverages:
            [
                new Coverage(CoverageType.Liability, 30_000m, 500m)
            ]);

        var p1003 = SeedActivePolicy(
            id: "P-1003", customer: rita, calculator: calculator, clock: clock,
            effectiveDaysAgo: 180, termDays: 365, renewalCount: 5,
            coverages:
            [
                new Coverage(CoverageType.Liability, 100_000m, 1_000m),
                new Coverage(CoverageType.Collision, 50_000m, 1_000m),
                new Coverage(CoverageType.Comprehensive, 50_000m, 1_000m)
            ]);

        var p1004 = Policy.Rehydrate(
            id: new PolicyId("P-1004"),
            customerId: anna.Id,
            status: PolicyStatus.Cancelled,
            effectiveDate: today.AddDays(-400),
            expiryDate: today.AddDays(-35),
            annualPremium: 540m,
            cancellationRefund: 45m,
            cancelledAtUtc: clock.UtcNow.AddDays(-30),
            cancellationReason: "Moved out of state",
            renewalCount: 1,
            coverages: [new Coverage(CoverageType.Liability, 25_000m, 500m)]);

        var p1005 = new Policy(new PolicyId("P-1005"), anna.Id);
        p1005.AddCoverage(new Coverage(CoverageType.Liability, 40_000m, 500m));
        p1005.AddCoverage(new Coverage(CoverageType.Collision, 20_000m, 500m));

        var p1006 = Policy.Rehydrate(
            id: new PolicyId("P-1006"),
            customerId: rita.Id,
            status: PolicyStatus.Expired,
            effectiveDate: today.AddDays(-400),
            expiryDate: today.AddDays(-35),
            annualPremium: 1_240m,
            cancellationRefund: null,
            cancelledAtUtc: null,
            cancellationReason: null,
            renewalCount: 4,
            coverages: [new Coverage(CoverageType.Liability, 80_000m, 500m)]);

        db.Policies.AddRange(p1001, p1002, p1003, p1004, p1005, p1006);

        var c2001 = new Claim(
            new ClaimId("C-2001"), p1003.Id,
            today.AddDays(-100), 1_200m, "Hail damage to windshield",
            requiresInspection: false, clock);

        var c2002 = new Claim(
            new ClaimId("C-2002"), p1001.Id,
            today.AddDays(-200), 3_500m, "Rear-ended at a stoplight; minor cosmetic",
            requiresInspection: true, clock);

        db.Claims.AddRange(c2001, c2002);

        db.SaveChanges();

        foreach (var p in db.Policies)
            p.ClearDomainEvents();
    }

    private static Policy SeedActivePolicy(
        string id, Customer customer, PremiumCalculator calculator, IClock clock,
        int effectiveDaysAgo, int termDays, int renewalCount, IEnumerable<Coverage> coverages)
    {
        var policy = new Policy(new PolicyId(id), customer.Id);
        foreach (var c in coverages) policy.AddCoverage(c);
        policy.Activate(customer, calculator, clock);

        return Policy.Rehydrate(
            id: policy.Id,
            customerId: policy.CustomerId,
            status: PolicyStatus.Active,
            effectiveDate: clock.Today.AddDays(-effectiveDaysAgo),
            expiryDate: clock.Today.AddDays(-effectiveDaysAgo + termDays),
            annualPremium: policy.AnnualPremium,
            cancellationRefund: null,
            cancelledAtUtc: null,
            cancellationReason: null,
            renewalCount: renewalCount,
            coverages: policy.Coverages);
    }
}
