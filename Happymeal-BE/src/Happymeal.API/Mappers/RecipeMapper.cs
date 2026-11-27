using Happymeal.API.Dtos;
using Happymeal.Application.Features.Recipes.Commands.CreateRecipe;
using Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;

namespace Happymeal.API.Mappers;

public static class RecipeMapper
{
    public static CreateRecipeFeature.Command MapToCommand(
        CreateRecipeRequestDto request)
    {
        return new CreateRecipeFeature.Command(
            Id: request.Id,
            Name: request.Name,
            Description: request.Description,
            Ingredients: request.Ingredients,
            Steps: request.Steps,
            Servings: request.Servings,
            PreparationMinutes: request.PreparationMinutes,
            Tags: request.Tags);
    }

    public static UpdateRecipeFeature.Command MapToCommand(
        UpdateRecipeRequestDto request,
        Guid id)
    {
        return new UpdateRecipeFeature.Command(
            Id: id,
            Name: request.Name,
            Description: request.Description,
            Ingredients: request.Ingredients,
            Steps: request.Steps,
            Servings: request.Servings,
            PreparationMinutes: request.PreparationMinutes,
            Tags: request.Tags);
    }
}
