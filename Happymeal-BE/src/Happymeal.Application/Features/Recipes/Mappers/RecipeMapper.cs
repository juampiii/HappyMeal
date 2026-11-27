using Happymeal.Application.Features.Recipes.Commands.CreateRecipe;
using Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;
using Happymeal.Domain.Params;

namespace Happymeal.Application.Features.Recipes.Mappers;

public static class RecipeMapper
{
    public static RecipeParams MapToParams(
        CreateRecipeFeature.Command command)
    {
        return new RecipeParams(
            Name: command.Name,
            Description: command.Description,
            Ingredients: command.Ingredients,
            Steps: command.Steps,
            Servings: command.Servings,
            PreparationMinutes: command.PreparationMinutes,
            Tags: command.Tags);
    }

    public static RecipeParams MapToParams(
        UpdateRecipeFeature.Command command)
    {
        return new RecipeParams(
            Name: command.Name,
            Description: command.Description,
            Ingredients: command.Ingredients,
            Steps: command.Steps,
            Servings: command.Servings,
            PreparationMinutes: command.PreparationMinutes,
            Tags: command.Tags);
    }
}
