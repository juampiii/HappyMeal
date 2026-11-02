using Happymeal.Application.Common.Messaging;

namespace Happymeal.Application.Features.Recipes.Commands.CreateRecipe;

/// <param name="Id">Optional id chosen by the client. The handler generates one when it is null.</param>
public sealed record CreateRecipeCommand(Guid? Id, string Name, string? Description) : ICommand<RecipeDto>;
