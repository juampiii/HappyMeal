namespace Happymeal.API.Dtos;

public sealed record ValidationErrorDto(
    string Code,
    IReadOnlyDictionary<string, object> Parameters);
