using Happymeal.Domain.Params;
namespace Happymeal.API.Dtos;

/// <param name="Id">Optional. Send it to choose the id on the client; a repeated id returns 409 Conflict.</param>
public sealed record CreateRecipeRequestDto(
    Guid? Id,
    string Name,
    string? Description = null,
    IReadOnlyList<IngredientParams>? Ingredients = null,
    IReadOnlyList<string>? Steps = null,
    int? Servings = null,
    int? PreparationMinutes = null,
    IReadOnlyList<string>? Tags = null);

public sealed record UpdateRecipeRequestDto(
    string Name,
    string? Description = null,
    IReadOnlyList<IngredientParams>? Ingredients = null,
    IReadOnlyList<string>? Steps = null,
    int? Servings = null,
    int? PreparationMinutes = null,
    IReadOnlyList<string>? Tags = null);
