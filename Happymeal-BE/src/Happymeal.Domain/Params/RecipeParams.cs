using Happymeal.Common.Validation;

namespace Happymeal.Domain.Params;

public sealed record RecipeParams(string Name, string? Description) : IValidatable
{
    public IEnumerable<ValidationFailure> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationFailure(nameof(Name), "Name is required.");
        }
        else if (Name.Length > Recipe.NameMaxLength)
        {
            yield return new ValidationFailure(nameof(Name), $"Name must be {Recipe.NameMaxLength} characters or fewer.");
        }

        if (Description?.Length > Recipe.DescriptionMaxLength)
        {
            yield return new ValidationFailure(nameof(Description),
                $"Description must be {Recipe.DescriptionMaxLength} characters or fewer.");
        }
    }
}
