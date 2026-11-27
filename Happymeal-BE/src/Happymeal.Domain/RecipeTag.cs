namespace Happymeal.Domain;

public sealed class RecipeTag
{
    public string Name { get; private set; } = null!;

    private RecipeTag() { }

    internal RecipeTag(string name)
    {
        Name = name;
    }
}
