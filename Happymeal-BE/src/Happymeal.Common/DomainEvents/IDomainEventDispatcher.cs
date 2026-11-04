using Happymeal.Common.Results;

namespace Happymeal.Common.DomainEvents;

public interface IDomainEventDispatcher
{
    Task<Result> DispatchAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        Guid correlationId,
        CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent;
}
