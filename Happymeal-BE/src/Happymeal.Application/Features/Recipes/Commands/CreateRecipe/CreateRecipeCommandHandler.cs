using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.Application.Features.Recipes.Commands.CreateRecipe;

public sealed class CreateRecipeCommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<CreateRecipeCommand, RecipeDto>
{
    public async Task<Result<RecipeDto>> HandleAsync(
        CreateRecipeCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = command.Id ?? Guid.CreateVersion7();

        var createResult = Recipe.Create(id, new RecipeParams(command.Name, command.Description));
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        // TODO: two concurrent requests with the same id can both pass this check; the second one fails on the primary key.
        if (await unitOfWork.RecipeRepository.ExistsAsync(id, cancellationToken))
        {
            return Error.Conflict($"Recipe {id} already exists.");
        }

        var recipe = createResult.Value;
        unitOfWork.RecipeRepository.Add(recipe);

        var wasSaved = await unitOfWork.SaveEntitiesAsync(cancellationToken);
        return !wasSaved
            ? Error.Failure("Recipe could not be persisted.")
            : Result.Success(RecipeDto.From(recipe));
    }
}
