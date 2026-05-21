using MediatR;
using Microsoft.Extensions.Logging;
using Sincera.Policies.Application.Common;
using Sincera.Policies.Domain.Policies.Events;

namespace Sincera.Policies.Application.Policies.EventHandlers;

public sealed class PolicyCancelledHandler : INotificationHandler<DomainEventNotification<PolicyCancelled>>
{
    private readonly ILogger<PolicyCancelledHandler> _logger;

    public PolicyCancelledHandler(ILogger<PolicyCancelledHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PolicyCancelled> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        _logger.LogInformation(
            "Policy {PolicyId} cancelled for customer {CustomerId}; effective {EffectiveCancellationDate}, refund {Refund:C}",
            ev.PolicyId.Value,
            ev.CustomerId.Value,
            ev.EffectiveCancellationDate,
            ev.Refund);
        return Task.CompletedTask;
    }
}
