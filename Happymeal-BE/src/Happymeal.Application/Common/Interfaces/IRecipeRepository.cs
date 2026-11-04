using Happymeal.Domain;

namespace Happymeal.Application.Common.Interfaces;

public interface IRecipeRepository
{
    /// <summary>Returns a tracked recipe, so changes and domain events are picked up by <see cref="IUnitOfWork.SaveEntitiesAsync"/>.</summary>
    Task<Recipe?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default);

    void Add(Recipe recipe);

    void Delete(Recipe recipe);
}
