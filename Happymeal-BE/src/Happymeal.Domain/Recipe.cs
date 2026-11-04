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
        recipe.AddDomainEvent(new RecipeCreatedDomainEvent(recipe.Id, recipe.Name));

        return Result.Success(recipe);
    }

    public Result Update(RecipeParams parameters)
    {
        var normalized = Normalize(parameters);
        if (normalized.Validate().ToValidationError() is { } error)
        {
            return error;
        }

        Name = normalized.Name;
        Description = normalized.Description;
        AddDomainEvent(new RecipeUpdatedDomainEvent(Id, Name));

        return Result.Success();
    }

    public void MarkAsDeleted()
    {
        AddDomainEvent(new RecipeDeletedDomainEvent(Id));
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

        return new RecipeParams(parameters.Name?.Trim() ?? string.Empty, description);
    }

    private Recipe(Guid id) : base(id)
    {
    }
}
