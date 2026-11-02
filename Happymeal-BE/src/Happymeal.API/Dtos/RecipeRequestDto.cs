namespace Happymeal.API.Dtos;

/// <param name="Id">Optional. Send it to choose the id on the client; a repeated id returns 409 Conflict.</param>
public sealed record CreateRecipeRequestDto(Guid? Id, string Name, string? Description);

public sealed record UpdateRecipeRequestDto(string Name, string? Description);
