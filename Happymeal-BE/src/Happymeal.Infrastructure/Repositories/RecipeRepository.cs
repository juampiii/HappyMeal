using Happymeal.Application.Common.Interfaces;
using Happymeal.Domain;
using Happymeal.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.Infrastructure.Repositories;

internal sealed class RecipeRepository(HappymealDbContext dbContext) : IRecipeRepository
{
    public Task<Recipe?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Recipes.AsTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Recipes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Recipes.AnyAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Recipes
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Recipe recipe)
    {
        dbContext.Recipes.Add(recipe);
    }

    public void Delete(Recipe recipe)
    {
        dbContext.Recipes.Remove(recipe);
    }
}
