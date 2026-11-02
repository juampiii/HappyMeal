using Happymeal.Application.Common.Messaging;

namespace Happymeal.Application.Features.Recipes.Queries.GetRecipeById;

public sealed record GetRecipeByIdQuery(Guid Id) : IQuery<RecipeDto>;
