using Microsoft.Extensions.Options;
using Moq;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Commands.ActivatePolicy;
using Sincera.Policies.Application.UnitTests.TestDoubles;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;
using Xunit;

namespace Sincera.Policies.Application.UnitTests.Policies;

public class ActivatePolicyHandlerTests
{
    private static readonly DateOnly Today = new(2026, 5, 20);

    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly FakeClock _clock = new(Today);
    private readonly IOptions<PremiumOptions> _options = Options.Create(new PremiumOptions());

    [Fact]
    public async Task Activates_draft_policy_and_returns_response()
    {
        var (policy, customer) = SeedDraftPolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _customers.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var handler = NewHandler();
        var response = await handler.Handle(new ActivatePolicyCommand(policy.Id), CancellationToken.None);

        Assert.Equal(policy.Id.Value, response.PolicyId);
        Assert.Equal(Today, response.EffectiveDate);
        Assert.Equal(Today.AddYears(1), response.ExpiryDate);
        Assert.True(response.AnnualPremium > 0);
        Assert.Equal(PolicyStatus.Active, policy.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Throws_when_policy_not_found()
    {
        _policies.Setup(r => r.GetByIdAsync(It.IsAny<PolicyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);

        var handler = NewHandler();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new ActivatePolicyCommand(new PolicyId("nope")), CancellationToken.None));

        Assert.Equal("policy.not_found", ex.Code);
    }

    [Fact]
    public async Task Throws_when_customer_not_found()
    {
        var (policy, _) = SeedDraftPolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _customers.Setup(r => r.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = NewHandler();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new ActivatePolicyCommand(policy.Id), CancellationToken.None));

        Assert.Equal("customer.not_found", ex.Code);
    }

    [Fact]
    public async Task Does_not_save_when_activation_fails()
    {
        var customer = NewCustomer();
        var policy = new Policy(new PolicyId("P-empty"), customer.Id);
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _customers.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var handler = NewHandler();

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new ActivatePolicyCommand(policy.Id), CancellationToken.None));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private ActivatePolicyCommandHandler NewHandler() =>
        new(_policies.Object, _customers.Object, _uow.Object, _clock, _options);

    private static (Policy policy, Customer customer) SeedDraftPolicy()
    {
        var customer = NewCustomer();
        var policy = new Policy(new PolicyId("P-1"), customer.Id);
        policy.AddCoverage(new Coverage(CoverageType.Liability, 30_000m, 500m));
        return (policy, customer);
    }

    private static Customer NewCustomer() =>
        new(new CustomerId("C-1"), "Test", new DateOnly(1985, 1, 1),
            new DrivingHistory(10, 0), "LV-1050");
}
