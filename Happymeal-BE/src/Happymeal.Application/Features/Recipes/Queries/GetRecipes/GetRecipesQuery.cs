using Happymeal.Application.Common.Messaging;

namespace Happymeal.Application.Features.Recipes.Queries.GetRecipes;

public sealed record GetRecipesQuery : IQuery<IReadOnlyList<RecipeDto>>;
