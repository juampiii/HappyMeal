using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;

namespace Happymeal.Application.Features.Recipes.Queries.GetRecipes;

public static class GetRecipesFeature
{
    public sealed record Query : IQuery<IReadOnlyList<RecipeDto>>;

    public sealed class Handler(IUnitOfWork unitOfWork) : IQueryHandler<Query, IReadOnlyList<RecipeDto>>
    {
        public async Task<Result<IReadOnlyList<RecipeDto>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            // TODO: add paging and filtering when the list grows.
            var recipes = await unitOfWork.RecipeRepository.GetAllAsync(cancellationToken);
            IReadOnlyList<RecipeDto> dtos = recipes.Select(RecipeDto.From).ToList();
            return Result.Success(dtos);
        }
    }
}
