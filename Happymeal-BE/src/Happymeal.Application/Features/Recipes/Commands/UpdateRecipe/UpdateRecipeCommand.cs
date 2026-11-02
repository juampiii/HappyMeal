using Happymeal.Application.Common.Messaging;

namespace Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;

public sealed record UpdateRecipeCommand(Guid Id, string Name, string? Description) : ICommand<RecipeDto>;
