using Happymeal.Common.DomainEvents;
using Happymeal.Common.Results;
using Happymeal.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Happymeal.Application.Features.Recipes.DomainEventHandlers;

/// <summary>Reference outbox subscriber. Replace the logging with the real reaction (projection, integration event, ...).</summary>
public sealed class LogRecipeLifecycleDomainEventHandler(ILogger<LogRecipeLifecycleDomainEventHandler> logger) :
    IDomainEventHandler<RecipeCreatedDomainEvent>,
    IDomainEventHandler<RecipeUpdatedDomainEvent>,
    IDomainEventHandler<RecipePublishedDomainEvent>,
    IDomainEventHandler<RecipeHiddenDomainEvent>,
    IDomainEventHandler<RecipeDeletedDomainEvent>
{
    public Task<Result> HandleAsync(
        RecipeCreatedDomainEvent domainEvent,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Recipe {RecipeId} created with name {Name} (correlation {CorrelationId}).",
            domainEvent.RecipeId, domainEvent.Name, correlationId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> HandleAsync(
        RecipeUpdatedDomainEvent domainEvent,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Recipe {RecipeId} updated with name {Name} (correlation {CorrelationId}).",
            domainEvent.RecipeId, domainEvent.Name, correlationId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> HandleAsync(
        RecipeDeletedDomainEvent domainEvent,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Recipe {RecipeId} deleted (correlation {CorrelationId}).",
            domainEvent.RecipeId, correlationId);
        return Task.FromResult(Result.Success());
    }
    public Task<Result> HandleAsync(RecipePublishedDomainEvent domainEvent, Guid correlationId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Recipe {RecipeId} version {Version} published (correlation {CorrelationId}).", domainEvent.RecipeId, domainEvent.Version, correlationId);
        return Task.FromResult(Result.Success());
    }
    public Task<Result> HandleAsync(RecipeHiddenDomainEvent domainEvent, Guid correlationId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Recipe {RecipeId} hidden (correlation {CorrelationId}).", domainEvent.RecipeId, correlationId);
        return Task.FromResult(Result.Success());
    }
}
