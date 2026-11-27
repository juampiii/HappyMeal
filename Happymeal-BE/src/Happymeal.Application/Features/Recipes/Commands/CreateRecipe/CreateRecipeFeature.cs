using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Application.Features.Recipes.Mappers;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.Application.Features.Recipes.Commands.CreateRecipe;

public static class CreateRecipeFeature
{
    /// <param name="Id">Optional id chosen by the client. The handler generates one when it is null.</param>
    public sealed record Command(
        Guid? Id,
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
            var id = command.Id ?? Guid.CreateVersion7();

            var parameters = RecipeMapper.MapToParams(command);
            var createResult = Recipe.Create(id, parameters);
            if (createResult.IsFailure)
            {
                return createResult.Error!;
            }

            if (await unitOfWork.RecipeRepository.ExistsAsync(id, cancellationToken))
            {
                return Error.Conflict($"Recipe {id} already exists.");
            }

            if (await unitOfWork.RecipeRepository.NameExistsAsync(createResult.Value.Name, id, cancellationToken))
            {
                return Error.Conflict("Recipe name already exists.");
            }

            var recipe = createResult.Value;
            unitOfWork.RecipeRepository.Add(recipe);

            var saved = await unitOfWork.TrySaveEntitiesAsync(cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error!;
            }
            var wasSaved = saved.Value;
            return !wasSaved
                ? Error.Failure("Recipe could not be persisted.")
                : Result.Success(RecipeDto.From(recipe));
        }
    }
}
