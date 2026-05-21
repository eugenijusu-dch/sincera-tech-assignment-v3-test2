using MediatR;
using Microsoft.Extensions.Logging;
using Sincera.Policies.Application.Common;
using Sincera.Policies.Domain.Policies.Events;

namespace Sincera.Policies.Application.Policies.EventHandlers;

public sealed class PolicyActivatedHandler : INotificationHandler<DomainEventNotification<PolicyActivated>>
{
    private readonly ILogger<PolicyActivatedHandler> _logger;

    public PolicyActivatedHandler(ILogger<PolicyActivatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PolicyActivated> notification, CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        _logger.LogInformation(
            "Policy {PolicyId} activated for customer {CustomerId} from {EffectiveDate} at premium {AnnualPremium:C}",
            ev.PolicyId.Value,
            ev.CustomerId.Value,
            ev.EffectiveDate,
            ev.AnnualPremium);
        return Task.CompletedTask;
    }
}
