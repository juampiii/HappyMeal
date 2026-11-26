using Happymeal.Domain;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;
using Happymeal.Domain.Params;

namespace Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;

public static class UpdateRecipeFeature
{
    public sealed record Command(
        Guid Id,
        string Name,
        string? Description = null,
        IReadOnlyList<IngredientParams>? Ingredients = null,
        IReadOnlyList<string>? Steps = null,
        int? Servings = null,
        int? PreparationMinutes = null,
        IReadOnlyList<string>? Tags = null) : ICommand<RecipeDto>;

    public sealed class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, RecipeDto>
    {
        public async Task<Result<RecipeDto>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            var recipe = await unitOfWork.RecipeRepository.GetAsync(command.Id, cancellationToken);
            if (recipe is null)
            {
                return Error.NotFound($"Recipe {command.Id} was not found.");
            }

            if (await unitOfWork.RecipeRepository.NameExistsAsync(RecipeName.Key(command.Name), command.Id, cancellationToken))
                return Error.Conflict("Recipe name already exists.");

            var updateResult = recipe.Update(new RecipeParams(command.Name, command.Description, command.Ingredients, command.Steps, command.Servings, command.PreparationMinutes, command.Tags));
            if (updateResult.IsFailure)
            {
                return updateResult.Error!;
            }

            var saved = await unitOfWork.TrySaveEntitiesAsync(cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error!;
            }
            if (!saved.Value)
            {
                return Error.Failure("Recipe could not be persisted.");
            }
            return Result.Success(RecipeDto.From(recipe));
        }
    }
}
