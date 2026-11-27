using System.Globalization;
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
            .Concat(ValidateTags())
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
        if (ingredients.Count > Recipe.IngredientsMaxCount)
        {
            yield return new ValidationFailure(nameof(Ingredients),
                $"A recipe can have at most {Recipe.IngredientsMaxCount} ingredients.");
        }
        var names = new HashSet<string>(StringComparer.Create(
            CultureInfo.InvariantCulture,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace));

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
            else if (!names.Add(ingredient.Name.Trim()))
            {
                yield return new ValidationFailure($"{path}.Name", "Ingredient is repeated.");
            }

            if (ingredient.Name?.Trim().Length > RecipeIngredient.NameMaxLength)
            {
                yield return new ValidationFailure($"{path}.Name",
                    $"Ingredient name must be {RecipeIngredient.NameMaxLength} characters or fewer.");
            }

            if (ingredient.Quantity is <= 0)
            {
                yield return new ValidationFailure($"{path}.Quantity", "Quantity must be positive.");
            }

            if (ingredient.Unit is not null)
            {
                var unitResult = UnitOfMeasure.Create(ingredient.Unit);
                if (unitResult.IsFailure)
                {
                    foreach (var message in unitResult.Error!.ValidationErrors.Values.SelectMany(messages => messages))
                    {
                        yield return new ValidationFailure($"{path}.Unit", message);
                    }
                }
            }
        }
    }

    private IEnumerable<ValidationFailure> ValidateSteps()
    {
        var steps = Steps ?? [];
        if (steps.Count > Recipe.StepsMaxCount)
        {
            yield return new ValidationFailure(nameof(Steps),
                $"A recipe can have at most {Recipe.StepsMaxCount} steps.");
        }

        for (var i = 0; i < steps.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(steps[i]))
            {
                yield return new ValidationFailure($"Steps[{i}]", "Step must have content.");
            }
            else if (steps[i].Trim().Length > RecipeStep.ContentMaxLength)
            {
                yield return new ValidationFailure($"Steps[{i}]",
                    $"Step content must be {RecipeStep.ContentMaxLength} characters or fewer.");
            }
        }
    }

    private IEnumerable<ValidationFailure> ValidateTags()
    {
        var tags = Tags ?? [];
        if (tags.Count > Recipe.TagsMaxInputCount)
        {
            yield return new ValidationFailure(nameof(Tags),
                $"A recipe request can contain at most {Recipe.TagsMaxInputCount} tag entries.");
            yield break;
        }

        var names = new HashSet<string>(RecipeTag.NameComparer);

        for (var i = 0; i < tags.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(tags[i]))
            {
                continue;
            }

            var name = tags[i].Trim();
            names.Add(name);
            if (name.Length > RecipeTag.NameMaxLength)
            {
                yield return new ValidationFailure($"Tags[{i}]",
                    $"Tag name must be {RecipeTag.NameMaxLength} characters or fewer.");
            }
        }

        if (names.Count > Recipe.TagsMaxCount)
        {
            yield return new ValidationFailure(nameof(Tags),
                $"A recipe can have at most {Recipe.TagsMaxCount} distinct tags.");
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
