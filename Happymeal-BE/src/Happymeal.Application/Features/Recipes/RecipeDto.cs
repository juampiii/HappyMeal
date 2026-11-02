using Happymeal.Domain;

namespace Happymeal.Application.Features.Recipes;

public sealed record RecipeDto(Guid Id, string Name, string? Description)
{
    public static RecipeDto From(Recipe recipe) => new(recipe.Id, recipe.Name, recipe.Description);
}
