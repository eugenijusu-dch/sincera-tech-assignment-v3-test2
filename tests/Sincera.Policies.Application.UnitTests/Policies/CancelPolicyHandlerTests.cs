using Microsoft.Extensions.Options;
using Moq;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Commands.CancelPolicy;
using Sincera.Policies.Application.UnitTests.TestDoubles;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Application.UnitTests.Policies;

public class CancelPolicyHandlerTests
{
    private static readonly DateOnly Today = new(2026, 5, 20);

    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly Mock<IClaimRepository> _claims = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly FakeClock _clock = new(Today);
    private readonly CancellationPolicy _cancellationPolicy = new();
    private readonly IOptions<CancellationOptions> _options =
        Options.Create(new CancellationOptions { AdminFee = 35m });

    [Fact]
    public async Task Cancels_active_policy_and_returns_refund()
    {
        var policy = ActivePolicy(renewalCount: 0);
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _claims.Setup(r => r.GetByPolicyIdAsync(policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var handler = NewHandler();
        var response = await handler.Handle(
            new CancelPolicyCommand(policy.Id, Today, "no longer needed"), CancellationToken.None);

        Assert.Equal(policy.Id.Value, response.PolicyId);
        Assert.True(response.Refund >= 0);
        Assert.Equal(PolicyStatus.Cancelled, policy.Status);
        Assert.Equal(response.Refund, policy.CancellationRefund);
        Assert.Equal("no longer needed", policy.CancellationReason);
        Assert.NotNull(policy.CancelledAtUtc);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Waives_admin_fee_for_loyal_claim_free_customer()
    {
        var loyal = ActivePolicy(renewalCount: 2);
        var newer = ActivePolicy(renewalCount: 0, id: "P-newer");
        _claims.Setup(r => r.GetByPolicyIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        _policies.Setup(r => r.GetByIdAsync(loyal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loyal);
        var loyalResp = await NewHandler().Handle(
            new CancelPolicyCommand(loyal.Id, Today, "x"), CancellationToken.None);

        _policies.Setup(r => r.GetByIdAsync(newer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newer);
        var newerResp = await NewHandler().Handle(
            new CancelPolicyCommand(newer.Id, Today, "x"), CancellationToken.None);

        Assert.Equal(35m, loyalResp.Refund - newerResp.Refund);
    }

    [Fact]
    public async Task Throws_when_policy_not_found()
    {
        _policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => NewHandler().Handle(
                new CancelPolicyCommand(new PolicyId("nope"), Today, "x"), CancellationToken.None));

        Assert.Equal("policy.not_found", ex.Code);
    }

    [Fact]
    public async Task Throws_invalid_transition_when_policy_not_active()
    {
        var policy = new Policy(new PolicyId("P-draft"), new CustomerId("C-1"));
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        await Assert.ThrowsAsync<InvalidPolicyTransitionException>(
            () => NewHandler().Handle(
                new CancelPolicyCommand(policy.Id, Today, "x"), CancellationToken.None));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Does_not_waive_fee_when_recent_claim_exists()
    {
        var loyal = ActivePolicy(renewalCount: 2);
        var recentClaim = new Claim(
            new ClaimId("CL-1"), loyal.Id, Today.AddMonths(-3),
            500m, "recent", false, _clock);
        _policies.Setup(r => r.GetByIdAsync(loyal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loyal);
        _claims.Setup(r => r.GetByPolicyIdAsync(loyal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim> { recentClaim });

        var fullPremium = loyal.AnnualPremium!.Value;
        var response = await NewHandler().Handle(
            new CancelPolicyCommand(loyal.Id, Today, "x"), CancellationToken.None);

        Assert.True(response.Refund < fullPremium);
    }

    private CancelPolicyCommandHandler NewHandler() => new(
        _policies.Object, _claims.Object, _uow.Object, _clock, _cancellationPolicy, _options);

    private static Policy ActivePolicy(int renewalCount, string id = "P-1")
    {
        var customer = new Customer(
            new CustomerId("C-1"), "Test", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var policy = Policy.Rehydrate(
            id: new PolicyId(id),
            customerId: customer.Id,
            status: PolicyStatus.Active,
            effectiveDate: Today.AddDays(-30),
            expiryDate: Today.AddDays(335),
            annualPremium: 1000m,
            cancellationRefund: null,
            cancelledAtUtc: null,
            cancellationReason: null,
            renewalCount: renewalCount,
            coverages: new[] { new Coverage(CoverageType.Liability, 30_000m, 500m) });
        return policy;
    }
}
