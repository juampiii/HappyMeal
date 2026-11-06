using Happymeal.Domain;

namespace Happymeal.Application.Common.Interfaces;

public interface IRecipeRepository
{
        Task<Recipe?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default);

    void Add(Recipe recipe);

    void Delete(Recipe recipe);
}
