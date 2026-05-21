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
    private readonly IOptions<CancellationOptions> _options = Options.Create(new CancellationOptions { AdminFee = 35m });

    [Fact]
    public async Task Cancels_active_policy_and_applies_admin_fee()
    {
        var (policy, _) = SeedActivePolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _claims.Setup(r => r.GetByPolicyIdAsync(policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var handler = NewHandler();
        var cancellationDate = Today.AddDays(30);
        var response = await handler.Handle(
            new CancelPolicyCommand(policy.Id, cancellationDate, "No longer needed"),
            CancellationToken.None);

        Assert.Equal(policy.Id.Value, response.PolicyId);
        Assert.Equal(cancellationDate, response.EffectiveCancellationDate);
        Assert.True(response.Refund >= 0);
        Assert.Equal(PolicyStatus.Cancelled, policy.Status);
        Assert.Equal("No longer needed", policy.CancellationReason);
        Assert.NotNull(policy.CancelledAtUtc);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Waives_admin_fee_for_loyal_claim_free_customer()
    {
        var (policy, _) = SeedActivePolicy(renewalCount: 3);
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _claims.Setup(r => r.GetByPolicyIdAsync(policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var handler = NewHandler();
        var cancellationDate = Today.AddDays(30);
        var response = await handler.Handle(
            new CancelPolicyCommand(policy.Id, cancellationDate, "Moving abroad"),
            CancellationToken.None);

        var withFee = SeedActivePolicy();
        _policies.Setup(r => r.GetByIdAsync(withFee.policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(withFee.policy);
        _claims.Setup(r => r.GetByPolicyIdAsync(withFee.policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());
        var responseFee = await handler.Handle(
            new CancelPolicyCommand(withFee.policy.Id, cancellationDate, "Moving abroad"),
            CancellationToken.None);

        Assert.True(response.Refund > responseFee.Refund,
            "Fee waiver customer should receive a higher refund than one who pays the admin fee.");
    }

    [Fact]
    public async Task Throws_InvalidPolicyTransition_for_non_active_policy()
    {
        var policy = new Policy(new PolicyId("P-draft"), new CustomerId("C-1"));
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var handler = NewHandler();

        await Assert.ThrowsAsync<InvalidPolicyTransitionException>(
            () => handler.Handle(
                new CancelPolicyCommand(policy.Id, Today, "reason"),
                CancellationToken.None));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Throws_when_policy_not_found()
    {
        _policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var handler = NewHandler();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new CancelPolicyCommand(new PolicyId("nope"), Today, "reason"),
                CancellationToken.None));

        Assert.Equal("policy.not_found", ex.Code);
    }

    [Fact]
    public async Task Raises_domain_event_on_cancellation()
    {
        var (policy, _) = SeedActivePolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _claims.Setup(r => r.GetByPolicyIdAsync(policy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var handler = NewHandler();
        await handler.Handle(
            new CancelPolicyCommand(policy.Id, Today.AddDays(10), "Sold vehicle"),
            CancellationToken.None);

        Assert.Contains(policy.DomainEvents,
            e => e is Sincera.Policies.Domain.Policies.Events.PolicyCancelled);
    }

    private CancelPolicyCommandHandler NewHandler() =>
        new(_policies.Object, _claims.Object, _uow.Object, _clock, new CancellationPolicy(), _options);

    private static (Policy policy, Customer customer) SeedActivePolicy(int renewalCount = 0)
    {
        var customer = new Customer(new CustomerId("C-1"), "Test", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
        var clock = new FakeClock(Today);
        var policy = new Policy(new PolicyId(Guid.NewGuid().ToString()), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        policy.Activate(customer, new PremiumCalculator(new PremiumOptions()), clock);
        if (renewalCount > 0)
        {
            var rehydrated = Policy.Rehydrate(
                policy.Id, policy.CustomerId, policy.Status,
                policy.EffectiveDate, policy.ExpiryDate,
                policy.AnnualPremium, policy.CancellationRefund,
                policy.CancelledAtUtc, policy.CancellationReason,
                renewalCount, policy.Coverages);
            return (rehydrated, customer);
        }
        return (policy, customer);
    }
}
