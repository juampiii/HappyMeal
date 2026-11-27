namespace Happymeal.Domain;

public static class RecipeErrorCodes
{
    public const string NotFound = "recipe.notFound";
    public const string NameAlreadyExists = "recipe.nameAlreadyExists";
    public const string IdAlreadyExists = "recipe.idAlreadyExists";
    public const string NotEditable = "recipe.notEditable";
    public const string Hidden = "recipe.hidden";
}
