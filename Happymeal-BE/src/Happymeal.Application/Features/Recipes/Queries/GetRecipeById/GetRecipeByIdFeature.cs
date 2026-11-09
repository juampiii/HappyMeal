using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;

namespace Happymeal.Application.Features.Recipes.Queries.GetRecipeById;

public static class GetRecipeByIdFeature
{
    public sealed record Query(Guid Id) : IQuery<RecipeDto>;

    public sealed class Handler(IUnitOfWork unitOfWork) : IQueryHandler<Query, RecipeDto>
    {
        public async Task<Result<RecipeDto>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var recipe = await unitOfWork.RecipeRepository.GetAsync(query.Id, cancellationToken);
            return recipe is null
                ? Error.NotFound($"Recipe {query.Id} was not found.")
                : Result.Success(RecipeDto.From(recipe));
        }
    }
}
