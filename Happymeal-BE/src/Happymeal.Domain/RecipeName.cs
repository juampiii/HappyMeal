using System.Globalization;
using System.Text;
namespace Happymeal.Domain;

public static class RecipeName
{
    public static string Key(string? value) => string.Concat((value ?? "").Trim()
        .Normalize(NormalizationForm.FormD).EnumerateRunes()
        .Where(r => Rune.GetUnicodeCategory(r) != UnicodeCategory.NonSpacingMark)
        .Select(r => Rune.ToUpperInvariant(r).ToString()));
}
