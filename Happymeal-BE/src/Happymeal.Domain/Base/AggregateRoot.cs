namespace Happymeal.Domain.Base;

/// <summary>Root of an aggregate. Only aggregate roots raise domain events.</summary>
public abstract class AggregateRoot(Guid id) : Entity(id)
{
    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>Returns the pending domain events and removes them from the aggregate.</summary>
    public IReadOnlyList<DomainEvent> PullDomainEvents()
    {
        var domainEvents = _domainEvents.ToList();
        _domainEvents.Clear();
        return domainEvents;
    }
}
