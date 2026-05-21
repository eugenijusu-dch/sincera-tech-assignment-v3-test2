using MediatR;
using Sincera.Policies.Domain.Common;

namespace Sincera.Policies.Application.Common;

public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : DomainEvent;
