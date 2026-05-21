using Microsoft.Extensions.Options;
using Moq;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Claims.Commands.FileClaim;
using Sincera.Policies.Application.UnitTests.TestDoubles;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Application.UnitTests.Claims;

public class FileClaimHandlerTests
{
    private static readonly DateOnly Today = new(2026, 5, 20);

    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly Mock<IClaimRepository> _claims = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly FakeClock _clock = new(Today);
    private readonly IOptions<ClaimsOptions> _options = Options.Create(new ClaimsOptions
    {
        InspectionThreshold = 5000m,
        FreshPolicyInspectionDays = 30
    });

    [Fact]
    public async Task Files_claim_and_returns_response()
    {
        var policy = SeedActivePolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var handler = NewHandler();
        var response = await handler.Handle(
            new FileClaimCommand(policy.Id, Today.AddDays(-5), 1000m, "Fender bender"),
            CancellationToken.None);

        Assert.NotNull(response.ClaimId);
        Assert.Equal(policy.Id.Value, response.PolicyId);
        Assert.Equal(1000m, response.ClaimedAmount);
        Assert.False(response.RequiresInspection);
        _claims.Verify(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sets_RequiresInspection_when_amount_exceeds_threshold()
    {
        var policy = SeedActivePolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var response = await NewHandler().Handle(
            new FileClaimCommand(policy.Id, Today.AddDays(-5), 6000m, "Major collision"),
            CancellationToken.None);

        Assert.True(response.RequiresInspection);
    }

    [Fact]
    public async Task Sets_RequiresInspection_for_fresh_policy_incident()
    {
        // Policy activated 25 days ago; incident 10 days ago — within the 30-day fresh window
        var effectiveDate = Today.AddDays(-25);
        var policy = Policy.Rehydrate(
            new PolicyId("P-fresh"), new CustomerId("C-1"), PolicyStatus.Active,
            effectiveDate, effectiveDate.AddYears(1), 342m, null, null, null, 0,
            new[] { new Coverage(CoverageType.Liability, 30_000m, 500m) });
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var incidentWithinWindow = Today.AddDays(-10);
        var response = await NewHandler().Handle(
            new FileClaimCommand(policy.Id, incidentWithinWindow, 100m, "Minor scratch"),
            CancellationToken.None);

        Assert.True(response.RequiresInspection);
    }

    [Fact]
    public async Task Throws_InvalidPolicyTransition_for_non_active_policy()
    {
        var policy = new Policy(new PolicyId("P-draft"), new CustomerId("C-1"));
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        await Assert.ThrowsAsync<InvalidPolicyTransitionException>(
            () => NewHandler().Handle(
                new FileClaimCommand(policy.Id, Today, 500m, "reason"),
                CancellationToken.None));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Throws_when_policy_not_found()
    {
        _policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => NewHandler().Handle(
                new FileClaimCommand(new PolicyId("nope"), Today, 500m, "desc"),
                CancellationToken.None));

        Assert.Equal("policy.not_found", ex.Code);
    }

    [Fact]
    public async Task Throws_for_future_incident_date()
    {
        var policy = SeedActivePolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        await Assert.ThrowsAsync<DomainException>(
            () => NewHandler().Handle(
                new FileClaimCommand(policy.Id, Today.AddDays(1), 500m, "future"),
                CancellationToken.None));
    }

    private FileClaimCommandHandler NewHandler() =>
        new(_policies.Object, _claims.Object, _uow.Object, _clock, _options);

    private Policy SeedActivePolicy()
    {
        var customer = new Customer(new CustomerId("C-1"), "Test", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var policy = new Policy(new PolicyId("P-1"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), _clock);
        return policy;
    }
}
