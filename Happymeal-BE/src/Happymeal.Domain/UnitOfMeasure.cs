using Happymeal.Common.Results;
using Happymeal.Common.Validation;

namespace Happymeal.Domain;

public sealed record UnitOfMeasure
{
    public static UnitOfMeasure Gram { get; } = new("g");
    public static UnitOfMeasure Kilogram { get; } = new("kg");
    public static UnitOfMeasure Millilitre { get; } = new("ml");
    public static UnitOfMeasure Litre { get; } = new("l");
    public static UnitOfMeasure Unit { get; } = new("ud");
    public static UnitOfMeasure Teaspoon { get; } = new("cucharadita");
    public static UnitOfMeasure Tablespoon { get; } = new("cucharada");

    private static readonly IReadOnlyDictionary<string, UnitOfMeasure> SupportedUnits =
        new[] { Gram, Kilogram, Millilitre, Litre, Unit, Teaspoon, Tablespoon }
            .ToDictionary(unit => unit.Code, StringComparer.Ordinal);

    public string Code { get; }

    private UnitOfMeasure(string code)
    {
        Code = code;
    }

    public static Result<UnitOfMeasure> Create(string? code)
    {
        if (code is not null && SupportedUnits.TryGetValue(code, out var unit))
        {
            return Result.Success(unit);
        }

        return Error.Validation([new ValidationFailure(
            nameof(Code), "Unit is not supported.", ValidationErrorCodes.UnsupportedValue)]);
    }

    public override string ToString() => Code;
}
