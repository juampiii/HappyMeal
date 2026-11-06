using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;
using Happymeal.Domain.Params;

namespace Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<UpdateRecipeCommand, RecipeDto>
{
    public async Task<Result<RecipeDto>> HandleAsync(
        UpdateRecipeCommand command,
        CancellationToken cancellationToken = default)
    {
        var recipeTracked = await unitOfWork.RecipeRepository.GetByIdTrackedAsync(command.Id, cancellationToken);
        if (recipeTracked is null)
        {
            return Error.NotFound($"Recipe {command.Id} was not found.");
        }

        var updateResult = recipeTracked.Update(new RecipeParams(command.Name, command.Description));
        if (updateResult.IsFailure)
        {
            return updateResult.Error!;
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success(RecipeDto.From(recipeTracked));
    }
}
