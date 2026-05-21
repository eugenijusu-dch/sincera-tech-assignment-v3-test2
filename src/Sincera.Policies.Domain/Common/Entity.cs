namespace Sincera.Policies.Domain.Common;

public abstract class Entity<TId> : IHasDomainEvents where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    public TId Id { get; protected set; } = default!;

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseEvent(DomainEvent @event) => _domainEvents.Add(@event);
}
