using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies.Events;

namespace Sincera.Policies.Domain.Policies;

public sealed class Policy : Entity<PolicyId>
{
    private readonly List<Coverage> _coverages = [];

    private Policy() { }

    public Policy(PolicyId id, CustomerId customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = PolicyStatus.Draft;
        RenewalCount = 0;
    }

    public CustomerId CustomerId { get; private set; }
    public PolicyStatus Status { get; private set; }
    public DateOnly? EffectiveDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public decimal? AnnualPremium { get; private set; }
    public decimal? CancellationRefund { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public int RenewalCount { get; private set; }

    public IReadOnlyList<Coverage> Coverages => _coverages;

    public void AddCoverage(Coverage coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        if (Status != PolicyStatus.Draft)
            throw new DomainException("policy.coverage_locked", "Coverage can only be modified while the policy is a draft.");
        _coverages.Add(coverage);
    }

    public void Activate(Customer customer, PremiumCalculator calculator, IClock clock)
    {
        if (Status != PolicyStatus.Draft)
            throw new InvalidPolicyTransitionException(Status, PolicyStatus.Active);
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(calculator);
        ArgumentNullException.ThrowIfNull(clock);
        if (!customer.Id.Equals(CustomerId))
            throw new DomainException("policy.customer_mismatch", "Customer does not own this policy.");
        if (_coverages.Count == 0)
            throw new DomainException("policy.no_coverage", "Policy must have at least one coverage to activate.");

        EffectiveDate = clock.Today;
        ExpiryDate = clock.Today.AddYears(1);
        AnnualPremium = calculator.Calculate(customer, _coverages, clock);
        Status = PolicyStatus.Active;

        RaiseEvent(new PolicyActivated(Id, CustomerId, EffectiveDate.Value, AnnualPremium.Value, clock.UtcNow));
    }

    public void Cancel(DateOnly effectiveCancellationDate, string reason, decimal refundAmount, IClock clock)
    {
        if (Status != PolicyStatus.Active)
            throw new InvalidPolicyTransitionException(Status, PolicyStatus.Cancelled);
        ArgumentNullException.ThrowIfNull(clock);

        Status = PolicyStatus.Cancelled;
        CancellationRefund = refundAmount;
        CancellationReason = reason;
        CancelledAtUtc = clock.UtcNow;

        RaiseEvent(new PolicyCancelled(Id, CustomerId, effectiveCancellationDate, refundAmount, clock.UtcNow));
    }

    public void MarkExpired(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status != PolicyStatus.Active)
            throw new InvalidPolicyTransitionException(Status, PolicyStatus.Expired);
        if (ExpiryDate is null || ExpiryDate.Value > clock.Today)
            throw new DomainException("policy.not_yet_expired", "Policy has not reached its expiry date.");

        Status = PolicyStatus.Expired;
    }

    public static Policy Rehydrate(
        PolicyId id,
        CustomerId customerId,
        PolicyStatus status,
        DateOnly? effectiveDate,
        DateOnly? expiryDate,
        decimal? annualPremium,
        decimal? cancellationRefund,
        DateTime? cancelledAtUtc,
        string? cancellationReason,
        int renewalCount,
        IEnumerable<Coverage> coverages)
    {
        var policy = new Policy
        {
            Id = id,
            CustomerId = customerId,
            Status = status,
            EffectiveDate = effectiveDate,
            ExpiryDate = expiryDate,
            AnnualPremium = annualPremium,
            CancellationRefund = cancellationRefund,
            CancelledAtUtc = cancelledAtUtc,
            CancellationReason = cancellationReason,
            RenewalCount = renewalCount
        };
        policy._coverages.AddRange(coverages);
        return policy;
    }
}
