using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;

namespace Happymeal.Application.Features.Recipes.Commands.DeleteRecipe;

public sealed class DeleteRecipeCommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<DeleteRecipeCommand>
{
    public async Task<Result> HandleAsync(DeleteRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var recipe = await unitOfWork.RecipeRepository.GetAsync(command.Id, cancellationToken);
        if (recipe is null)
        {
            return Error.NotFound($"Recipe {command.Id} was not found.");
        }

        recipe.MarkAsDeleted();
        unitOfWork.RecipeRepository.Delete(recipe);

        var wasSaved = await unitOfWork.SaveEntitiesAsync(cancellationToken);
        return !wasSaved
            ? Error.Failure("Recipe could not be deleted.")
            : Result.Success();
    }
}
