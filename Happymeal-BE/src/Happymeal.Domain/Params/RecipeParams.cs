using Happymeal.Common.Validation;

namespace Happymeal.Domain.Params;

public sealed record IngredientParams(string Name, decimal? Quantity = null, string? Unit = null);

public sealed record RecipeParams(
    string Name,
    string? Description = null,
    IReadOnlyList<IngredientParams>? Ingredients = null,
    IReadOnlyList<string>? Steps = null,
    int? Servings = null,
    int? PreparationMinutes = null,
    IReadOnlyList<string>? Tags = null) : IValidatable
{
    public IEnumerable<ValidationFailure> Validate()
    {
        return ValidateName()
            .Concat(ValidateNumbers())
            .Concat(ValidateIngredients())
            .Concat(ValidateSteps())
            .Concat(ValidateDescription());
    }

    private IEnumerable<ValidationFailure> ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationFailure(nameof(Name), "Name is required.");
        }
        else if (Name.Length > Recipe.NameMaxLength)
        {
            yield return new ValidationFailure(nameof(Name), $"Name must be {Recipe.NameMaxLength} characters or fewer.");
        }
    }

    private IEnumerable<ValidationFailure> ValidateNumbers()
    {
        if (Servings is <= 0)
        {
            yield return new ValidationFailure(nameof(Servings), "Servings must be positive.");
        }

        if (PreparationMinutes is <= 0)
        {
            yield return new ValidationFailure(nameof(PreparationMinutes), "Preparation time must be positive.");
        }
    }

    private IEnumerable<ValidationFailure> ValidateIngredients()
    {
        var ingredients = Ingredients ?? [];
        var names = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < ingredients.Count; i++)
        {
            var path = $"Ingredients[{i}]";
            var ingredient = ingredients[i];

            if (ingredient is null)
            {
                yield return new ValidationFailure(path, "Ingredient is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(ingredient.Name))
            {
                yield return new ValidationFailure($"{path}.Name", "Name is required.");
            }
            else if (!names.Add(RecipeName.Key(ingredient.Name)))
            {
                yield return new ValidationFailure($"{path}.Name", "Ingredient is repeated.");
            }

            if (ingredient.Quantity is <= 0)
            {
                yield return new ValidationFailure($"{path}.Quantity", "Quantity must be positive.");
            }

            if (ingredient.Unit is not null && !RecipeUnits.Codes.Contains(ingredient.Unit))
            {
                yield return new ValidationFailure($"{path}.Unit", "Unit is not supported.");
            }
        }
    }

    private IEnumerable<ValidationFailure> ValidateSteps()
    {
        var steps = Steps ?? [];

        for (var i = 0; i < steps.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(steps[i]))
            {
                yield return new ValidationFailure($"Steps[{i}]", "Step must have content.");
            }
        }
    }

    private IEnumerable<ValidationFailure> ValidateDescription()
    {
        if (Description?.Length > Recipe.DescriptionMaxLength)
        {
            yield return new ValidationFailure(nameof(Description),
                $"Description must be {Recipe.DescriptionMaxLength} characters or fewer.");
        }
    }
}
