namespace Happymeal.Application.Common.Interfaces;

public interface IUnitOfWork
{
    public IRecipeRepository RecipeRepository { get; }

    /// <summary>Stages pending domain events in the outbox and saves everything in one transaction.</summary>
    public Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
