using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Common.Messaging;
using Happymeal.Common.Results;

namespace Happymeal.Application.Features.Recipes.Queries.GetRecipeById;

public sealed class GetRecipeByIdQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetRecipeByIdQuery, RecipeDto>
{
    public async Task<Result<RecipeDto>> HandleAsync(
        GetRecipeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var recipe = await unitOfWork.RecipeRepository.GetAsync(query.Id, cancellationToken);
        return recipe is null
            ? Error.NotFound($"Recipe {query.Id} was not found.")
            : Result.Success(RecipeDto.From(recipe));
    }
}
