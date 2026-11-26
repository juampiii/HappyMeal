using Happymeal.Common.Results;

namespace Happymeal.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<Result<bool>> TrySaveEntitiesAsync(CancellationToken cancellationToken = default);

    public IRecipeRepository RecipeRepository { get; }

    /// <summary>Stages pending domain events in the outbox and saves everything in one transaction.</summary>
    public Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
