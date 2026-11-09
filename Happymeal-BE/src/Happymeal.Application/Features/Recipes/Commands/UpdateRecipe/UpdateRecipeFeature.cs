using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;
using Happymeal.Domain.Params;

namespace Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;

public static class UpdateRecipeFeature
{
    public sealed record Command(Guid Id, string Name, string? Description) : ICommand<RecipeDto>;

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

            var updateResult = recipe.Update(new RecipeParams(command.Name, command.Description));
            if (updateResult.IsFailure)
            {
                return updateResult.Error!;
            }

            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return Result.Success(RecipeDto.From(recipe));
        }
    }
}
