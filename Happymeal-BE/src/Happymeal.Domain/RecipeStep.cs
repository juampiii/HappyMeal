namespace Happymeal.Domain;

public sealed class RecipeStep
{
    public const int ContentMaxLength = 2000;

    public int Position { get; private set; }
    public string Content { get; private set; } = null!;

    private RecipeStep() { }

    internal RecipeStep(
        int position,
        string content)
    {
        Position = position;
        Content = content;
    }
}
