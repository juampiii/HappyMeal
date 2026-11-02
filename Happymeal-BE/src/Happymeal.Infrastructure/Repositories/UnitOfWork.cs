using Happymeal.Application.Common.Interfaces;
using Happymeal.Domain.Base;
using Happymeal.Infrastructure.Database;
using Happymeal.Infrastructure.Outbox;

namespace Happymeal.Infrastructure.Repositories;

internal sealed class UnitOfWork(
    HappymealDbContext dbContext,
    TimeProvider timeProvider,
    IRecipeRepository recipeRepository
) : IUnitOfWork
{
    public IRecipeRepository RecipeRepository => recipeRepository;

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        StageDomainEvents();
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    private void StageDomainEvents()
    {
        var domainEvents = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.PullDomainEvents())
            .ToList();

        if (domainEvents.Count == 0)
        {
            return;
        }

        var occurredOn = timeProvider.GetUtcNow();
        var outboxMessages = domainEvents.Select(domainEvent => OutboxMessage.Create(domainEvent, occurredOn));

        dbContext.OutboxMessages.AddRange(outboxMessages);
    }
}
