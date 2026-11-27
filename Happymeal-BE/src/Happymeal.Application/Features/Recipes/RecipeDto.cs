using Happymeal.Domain.Params;
using Happymeal.Domain;

namespace Happymeal.Application.Features.Recipes;

public sealed record RecipeDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<IngredientParams>? Ingredients = null,
    IReadOnlyList<string>? Steps = null,
    int? Servings = null,
    int? PreparationMinutes = null,
    IReadOnlyList<string>? Tags = null,
    bool IsPublished = false,
    bool IsHidden = false,
    int Version = 1)
{
    public static RecipeDto From(Recipe recipe) => new(recipe.Id, recipe.Name, recipe.Description,
        [.. recipe.Ingredients.OrderBy(x => x.Position).Select(x => new IngredientParams(x.Name, x.Quantity, x.Unit?.Code))],
        [.. recipe.Steps.Select(step => step.Content)], recipe.Servings, recipe.PreparationMinutes,
        [.. recipe.Tags.Select(tag => tag.Name)],
        recipe.IsPublished, recipe.IsHidden, recipe.Version);
}
