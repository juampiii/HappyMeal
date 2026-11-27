using Happymeal.Application.Common.Interfaces;
using Happymeal.Domain;
using Happymeal.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.Infrastructure.Repositories;

internal sealed class RecipeRepository(HappymealDbContext dbContext) : IRecipeRepository
{
    public Task<Recipe?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Recipes.Include(r => r.Ingredients).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Recipes.AnyAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Recipes
            .Include(r => r.Ingredients)
            .Where(r => !r.IsHidden)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Recipe recipe)
    {
        dbContext.Recipes.Add(recipe);
    }

    public Task<bool> NameExistsAsync(string name, Guid exceptId, CancellationToken cancellationToken = default)
        => dbContext.Recipes.AnyAsync(r => r.Id != exceptId && r.Name == name, cancellationToken);
}
