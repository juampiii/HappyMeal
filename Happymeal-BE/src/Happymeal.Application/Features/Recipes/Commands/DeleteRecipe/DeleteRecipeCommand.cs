using Happymeal.Application.Common.Messaging;

namespace Happymeal.Application.Features.Recipes.Commands.DeleteRecipe;

public sealed record DeleteRecipeCommand(Guid Id) : ICommand;
