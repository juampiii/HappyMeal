using Happymeal.Common.Results;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

    public async Task<Result<bool>> TrySaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        try { return Result.Success(await SaveEntitiesAsync(cancellationToken)); }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Error.Conflict("Recipe changed concurrently. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            return Error.Conflict("Recipe name or id already exists.");
        }
    }

    private void StageDomainEvents()
    {
        // An edit to owned elements must also update the root so xmin protects
        // the complete aggregate against a concurrent publication.
        foreach (var entry in dbContext.ChangeTracker.Entries<AggregateRoot>())
        {
            if (entry.State == EntityState.Unchanged && entry.Entity.GetDomainEvents().Count != 0)
            {
                entry.State = EntityState.Modified;
            }
        }

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
