using MediatR;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Common;
using Sincera.Policies.Domain.Common;

namespace Sincera.Policies.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PoliciesDbContext _db;
    private readonly IMediator _mediator;

    public UnitOfWork(PoliciesDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var aggregates = _db.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasDomainEvents h && h.DomainEvents.Count > 0)
            .Select(e => (IHasDomainEvents)e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        var changes = await _db.SaveChangesAsync(cancellationToken);

        foreach (var ev in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(ev.GetType());
            var notification = Activator.CreateInstance(notificationType, ev)!;
            await _mediator.Publish(notification, cancellationToken);
        }

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        return changes;
    }
}
