using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Customers;

namespace Sincera.Policies.Domain.Policies.Events;

public sealed record PolicyCancelled(
    PolicyId PolicyId,
    CustomerId CustomerId,
    DateOnly EffectiveCancellationDate,
    decimal RefundAmount,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
