namespace Happymeal.Domain;

public sealed class RecipeIngredient
{
    public const int NameMaxLength = 200;

    public int Position { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal? Quantity { get; private set; }
    public UnitOfMeasure? Unit { get; private set; }
    private RecipeIngredient() { }
    internal RecipeIngredient(
        int position,
        string name,
        decimal? quantity,
        UnitOfMeasure? unit)
    {
        Position = position;
        Name = name;
        Quantity = quantity;
        Unit = unit;
    }
}
