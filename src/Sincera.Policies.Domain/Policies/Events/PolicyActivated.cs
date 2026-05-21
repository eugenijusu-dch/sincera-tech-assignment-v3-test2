using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Customers;

namespace Sincera.Policies.Domain.Policies.Events;

public sealed record PolicyActivated(
    PolicyId PolicyId,
    CustomerId CustomerId,
    DateOnly EffectiveDate,
    decimal AnnualPremium,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
