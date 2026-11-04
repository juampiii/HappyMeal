using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;

namespace Happymeal.Application.Features.Recipes.Queries.GetRecipes;

public sealed class GetRecipesQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetRecipesQuery, IReadOnlyList<RecipeDto>>
{
    public async Task<Result<IReadOnlyList<RecipeDto>>> HandleAsync(
        GetRecipesQuery query,
        CancellationToken cancellationToken = default)
    {
        // TODO: add paging and filtering when the list grows.
        var recipes = await unitOfWork.RecipeRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<RecipeDto> dtos = recipes.Select(RecipeDto.From).ToList();
        return Result.Success(dtos);
    }
}
