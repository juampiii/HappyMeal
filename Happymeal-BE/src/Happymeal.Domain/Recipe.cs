using Happymeal.Common.Results;
using Happymeal.Common.Validation;
using Happymeal.Domain.Base;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.Domain;

public sealed class Recipe : AggregateRoot, IAuditable
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public bool IsPublished { get; private set; }
    public bool IsHidden { get; private set; }
    public int Version { get; private set; } = 1;
    public uint Revision { get; private set; }
    private List<RecipeIngredient> _ingredients = [];
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();
    private List<RecipeStep> _steps = [];
    public IReadOnlyList<RecipeStep> Steps => _steps.OrderBy(step => step.Position).ToList().AsReadOnly();
    private List<RecipeTag> _tags = [];
    public IReadOnlyList<RecipeTag> Tags => _tags.AsReadOnly();
    public int? Servings { get; private set; }
    public int? PreparationMinutes { get; private set; }

    public DateTimeOffset CreatedOn { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? ModifiedOn { get; private set; }
    public string? ModifiedBy { get; private set; }

    /// <summary>Creates a recipe. The caller chooses the id, so a client can send its own one.</summary>
    public static Result<Recipe> Create(Guid id, RecipeParams parameters)
    {
        var normalized = Normalize(parameters);
        if (ValidateId(id).Concat(normalized.Validate()).ToValidationError() is { } error)
        {
            return error;
        }

        var recipe = new Recipe(id)
        {
            Name = normalized.Name,
            Description = normalized.Description
        };
        recipe.Apply(normalized);
        recipe.AddDomainEvent(new RecipeCreatedDomainEvent(recipe.Id, recipe.Name));

        return Result.Success(recipe);
    }

    public Result Update(RecipeParams parameters)
    {
        if (IsPublished || IsHidden)
        {
            return Error.Conflict("Recipe content cannot be edited in its current state.");
        }
        var normalized = Normalize(parameters);
        if (normalized.Validate().ToValidationError() is { } error)
        {
            return error;
        }

        Apply(normalized);
        AddDomainEvent(new RecipeUpdatedDomainEvent(Id, Name));

        return Result.Success();
    }

    public Result Publish()
    {
        if (IsHidden)
        {
            return Error.Conflict("Hidden recipes cannot be published.");
        }
        if (IsPublished)
        {
            return Result.Success();
        }
        var failures = new List<ValidationFailure>();
        if (_ingredients.Count == 0)
        {
            failures.Add(new(nameof(Ingredients), "At least one ingredient is required."));
        }
        if (Steps.Count == 0)
        {
            failures.Add(new(nameof(Steps), "At least one step is required."));
        }
        if (failures.ToValidationError() is { } error)
        {
            return error;
        }
        IsPublished = true;
        AddDomainEvent(new RecipePublishedDomainEvent(Id, Version));
        return Result.Success();
    }

    public Result Hide()
    {
        if (IsHidden)
        {
            return Result.Success();
        }
        IsHidden = true;
        AddDomainEvent(new RecipeHiddenDomainEvent(Id));
        return Result.Success();
    }

    private void Apply(RecipeParams parameters)
    {
        Name = parameters.Name;
        Description = parameters.Description;
        _ingredients = [.. (parameters.Ingredients ?? [])
            .Select((ingredient, position) => new RecipeIngredient(
                position: position,
                name: ingredient.Name.Trim(),
                quantity: ingredient.Quantity,
                unit: ingredient.Unit is null ? null : UnitOfMeasure.Create(ingredient.Unit).Value))];
        _steps = [.. (parameters.Steps ?? [])
            .Select((content, position) => new RecipeStep(
                position: position,
                content: content.Trim()))];
        _tags = [.. (parameters.Tags ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct()
            .Select(name => new RecipeTag(name))];
        Servings = parameters.Servings;
        PreparationMinutes = parameters.PreparationMinutes;
    }

    private static IEnumerable<ValidationFailure> ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            yield return new ValidationFailure(nameof(Id), "Id must not be empty.");
        }
    }

    private static RecipeParams Normalize(RecipeParams parameters)
    {
        var description = string.IsNullOrWhiteSpace(parameters.Description)
            ? null
            : parameters.Description.Trim();

        return parameters with { Name = parameters.Name?.Trim() ?? string.Empty, Description = description };
    }

    private Recipe(Guid id) : base(id)
    {
    }
}
