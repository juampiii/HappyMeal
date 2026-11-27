using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain.Params;

public class RecipeContentLimitsTests
{
    [Fact]
    public void Validate_WhenTextExceedsLimits_ShouldReturnEachFieldFailure()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Ingredients: [new(new string('i', RecipeIngredient.NameMaxLength + 1))],
            Steps: [new string('s', RecipeStep.ContentMaxLength + 1)],
            Tags: [new string('t', RecipeTag.NameMaxLength + 1)]);

        // Act
        var failurePaths = parameters.Validate().Select(failure => failure.PropertyName);

        // Assert
        failurePaths.Should().BeEquivalentTo("Ingredients[0].Name", "Steps[0]", "Tags[0]");
    }

    [Fact]
    public void Create_WhenTextAndCollectionCountsMatchLimits_ShouldAcceptTrimmedContent()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Ingredients: [.. Enumerable.Range(0, Recipe.IngredientsMaxCount)
                .Select(index => new IngredientParams($" {index:D3}{new string('i', RecipeIngredient.NameMaxLength - 3)} "))],
            Steps: [.. Enumerable.Repeat($" {new string('s', RecipeStep.ContentMaxLength)} ", Recipe.StepsMaxCount)],
            Tags: [.. Enumerable.Range(0, Recipe.TagsMaxCount)
                .Select(index => $" {index:D3}{new string('t', RecipeTag.NameMaxLength - 3)} ")]);

        // Act
        var result = Recipe.Create(Guid.CreateVersion7(), parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ingredients.Should().HaveCount(Recipe.IngredientsMaxCount);
        result.Value.Ingredients.Should().OnlyContain(ingredient => ingredient.Name.Length == RecipeIngredient.NameMaxLength);
        result.Value.Steps.Should().HaveCount(Recipe.StepsMaxCount);
        result.Value.Steps.Should().OnlyContain(step => step.Content.Length == RecipeStep.ContentMaxLength);
        result.Value.Tags.Should().HaveCount(Recipe.TagsMaxCount);
        result.Value.Tags.Should().OnlyContain(tag => tag.Name.Length == RecipeTag.NameMaxLength);
    }

    [Fact]
    public void Validate_WhenCollectionCountsExceedLimits_ShouldReturnEachCollectionFailure()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Ingredients: [.. Enumerable.Range(0, Recipe.IngredientsMaxCount + 1)
                .Select(index => new IngredientParams($"Ingredient {index}"))],
            Steps: [.. Enumerable.Repeat("Cook", Recipe.StepsMaxCount + 1)],
            Tags: [.. Enumerable.Range(0, Recipe.TagsMaxCount + 1).Select(index => $"Tag {index}")]);

        // Act
        var failurePaths = parameters.Validate().Select(failure => failure.PropertyName);

        // Assert
        failurePaths.Should().BeEquivalentTo("Ingredients", "Steps", "Tags");
    }

    [Fact]
    public void Create_WhenTagsContainEmptyValuesAndCaseVariants_ShouldCountOnlyDistinctNonemptyTags()
    {
        // Arrange
        string[] tags = [.. Enumerable.Range(0, Recipe.TagsMaxCount).Select(index => $"Tag {index}")];
        var parameters = new RecipeParams(
            Name: "Soup",
            Tags: [.. tags, .. tags.Select(tag => tag.ToUpperInvariant()), "", " ", null!]);

        // Act
        var result = Recipe.Create(Guid.CreateVersion7(), parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Select(tag => tag.Name).Should().Equal(tags);
    }

    [Fact]
    public void Create_WhenRawTagCountMatchesLimit_ShouldIgnoreBlankAndRepeatedEntries()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Tags: [.. Enumerable.Repeat("", Recipe.TagsMaxInputCount - 2), "quick", "QUICK"]);

        // Act
        var result = Recipe.Create(Guid.CreateVersion7(), parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Select(tag => tag.Name).Should().Equal("quick");
    }

    [Fact]
    public void Validate_WhenRawTagCountExceedsLimit_ShouldRejectEvenBlankEntries()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Tags: [.. Enumerable.Repeat("", Recipe.TagsMaxInputCount + 1)]);

        // Act
        var failures = parameters.Validate();

        // Assert
        failures.Should().ContainSingle().Which.PropertyName.Should().Be("Tags");
    }

    [Fact]
    public void Update_WhenTagIsTooLong_ShouldKeepPreviousContentAndRaiseNoEvent()
    {
        // Arrange
        var original = new RecipeParams("Soup", Tags: ["quick"]);
        var recipe = Recipe.Create(Guid.CreateVersion7(), original).Value;
        recipe.PullDomainEvents();
        var invalid = original with { Name = "Changed", Tags = [new string('t', RecipeTag.NameMaxLength + 1)] };

        // Act
        var result = recipe.Update(invalid);

        // Assert
        result.IsFailure.Should().BeTrue();
        recipe.Name.Should().Be("Soup");
        recipe.Tags.Select(tag => tag.Name).Should().Equal("quick");
        recipe.GetDomainEvents().Should().BeEmpty();
    }
}
