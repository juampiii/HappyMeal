using Happymeal.Common.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Happymeal.Common.DomainEvents;

/// <summary>Runs every registered handler for a domain event. The first failure stops the dispatch.</summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task<Result> DispatchAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        Guid correlationId,
        CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();

        foreach (var handler in handlers)
        {
            var result = await handler.HandleAsync(domainEvent, correlationId, cancellationToken);
            if (result.IsFailure)
            {
                return Error.Failure($"{handler.GetType().Name}: {result.Error!.Message}");
            }
        }

        return Result.Success();
    }
}
