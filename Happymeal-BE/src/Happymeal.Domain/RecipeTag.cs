namespace Happymeal.Domain;

public sealed class RecipeTag
{
    public const int NameMaxLength = 50;
    internal static StringComparer NameComparer { get; } = StringComparer.OrdinalIgnoreCase;

    public string Name { get; private set; } = null!;

    private RecipeTag() { }

    internal RecipeTag(string name)
    {
        Name = name;
    }
}
