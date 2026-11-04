using Happymeal.Common.Results;

namespace Happymeal.Common.DomainEvents;

public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task<Result> HandleAsync(TDomainEvent domainEvent, Guid correlationId, CancellationToken cancellationToken = default);
}
