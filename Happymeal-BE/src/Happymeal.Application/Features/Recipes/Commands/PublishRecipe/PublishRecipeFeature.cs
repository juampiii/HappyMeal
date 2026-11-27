using Happymeal.Domain;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;
namespace Happymeal.Application.Features.Recipes.Commands.PublishRecipe;

public static class PublishRecipeFeature
{
    public sealed record Command(Guid Id) : ICommand;
    public sealed class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var recipe = await unitOfWork.RecipeRepository.GetAsync(command.Id, cancellationToken);
            if (recipe is null)
            {
                return Error.NotFound($"Recipe {command.Id} was not found.", RecipeErrorCodes.NotFound);
            }
            if (recipe.IsPublished && !recipe.IsHidden)
            {
                return Result.Success();
            }
            var result = recipe.Publish();
            if (result.IsFailure)
            {
                return result;
            }
            var saved = await unitOfWork.TrySaveEntitiesAsync(cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error!;
            }
            return saved.Value ? Result.Success() : Error.Failure("Recipe could not be persisted.", RecipeErrorCodes.SaveFailed);
        }
    }
}
