namespace Happymeal.Domain;

public sealed class RecipeIngredient
{
    public int Position { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    private RecipeIngredient() { }
    internal RecipeIngredient(int position, string name, decimal? quantity, string? unit)
        => (Position, Name, Quantity, Unit) = (position, name, quantity, unit);
}

public static class RecipeUnits
{
    public static IReadOnlyList<string> Codes { get; } = Array.AsReadOnly<string>(["g", "kg", "ml", "l", "ud", "cucharadita", "cucharada"]);
}
