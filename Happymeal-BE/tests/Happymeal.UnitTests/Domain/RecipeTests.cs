using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain;

public class RecipeTests
{
    private static Recipe CreateRecipe(string name = "Burger", string? description = null)
    {
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams(name, description)).Value;
        recipe.PullDomainEvents();
        return recipe;
    }

    [Fact]
    public void Create_WhenParamsAreValid_ShouldReturnRecipeAndRaiseRecipeCreatedDomainEvent()
    {
        // Arrange
        var id = Guid.CreateVersion7();

        // Act
        var result = Recipe.Create(id, new RecipeParams("Burger", "With fries"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.Name.Should().Be("Burger");
        result.Value.Description.Should().Be("With fries");

        var recipeCreated = result.Value.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<RecipeCreatedDomainEvent>().Subject;
        recipeCreated.RecipeId.Should().Be(id);
        recipeCreated.Name.Should().Be("Burger");
    }

    [Fact]
    public void Create_WhenStepsContainWhitespace_ShouldTrimContentAndKeepTheirPositions()
    {
        // Arrange
        var content = new RecipeParams(
            Name: "Soup",
            Steps: ["  Chop  ", "  Cook  ", "Serve"]);

        // Act
        var result = Recipe.Create(Guid.CreateVersion7(), content);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Select(step => step.Content).Should().Equal("Chop", "Cook", "Serve");
        result.Value.Steps.Select(step => step.Position).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Create_WhenTagsContainWhitespaceAndDuplicates_ShouldKeepDistinctNonemptyNames()
    {
        // Arrange
        var content = new RecipeParams(
            Name: "Soup",
            Tags: ["  quick  ", "", " ", "QUICK", "Quick", "vegan"]);

        // Act
        var result = Recipe.Create(Guid.CreateVersion7(), content);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Select(tag => tag.Name).Should().BeEquivalentTo("quick", "vegan");
    }

    [Fact]
    public void Create_WhenNameAndDescriptionHaveExtraSpaces_ShouldTrimThem()
    {
        var result = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("  Burger ", "  With fries  "));

        result.Value.Name.Should().Be("Burger");
        result.Value.Description.Should().Be("With fries");
    }

    [Fact]
    public void Create_WhenDescriptionIsWhitespace_ShouldStoreNullDescription()
    {
        var result = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", "   "));

        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WhenIdIsEmpty_ShouldReturnValidationFailure()
    {
        var result = Recipe.Create(Guid.Empty, new RecipeParams("Burger", null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(Recipe.Id));
    }

    [Fact]
    public void Create_WhenNameIsEmpty_ShouldReturnValidationFailure()
    {
        var result = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("", null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(RecipeParams.Name));
    }

    [Fact]
    public void Create_WhenIdAndNameAreInvalid_ShouldReturnBothValidationFailures()
    {
        var result = Recipe.Create(Guid.Empty, new RecipeParams("", null));

        result.Error!.ValidationErrors.Keys
            .Should().BeEquivalentTo([nameof(Recipe.Id), nameof(RecipeParams.Name)]);
    }

    [Fact]
    public void Update_WhenParamsAreValid_ShouldChangeStateAndRaiseRecipeUpdatedDomainEvent()
    {
        // Arrange
        var recipe = CreateRecipe("Burger", "With fries");

        // Act
        var result = recipe.Update(new RecipeParams("  Burger XL ", "  "));

        // Assert
        result.IsSuccess.Should().BeTrue();
        recipe.Name.Should().Be("Burger XL");
        recipe.Description.Should().BeNull();

        var recipeUpdated = recipe.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<RecipeUpdatedDomainEvent>().Subject;
        recipeUpdated.RecipeId.Should().Be(recipe.Id);
        recipeUpdated.Name.Should().Be("Burger XL");
    }

    [Fact]
    public void Update_WhenNameIsEmpty_ShouldReturnFailureAndKeepStateAndRaiseNoEvent()
    {
        // Arrange
        var recipe = CreateRecipe("Burger", "With fries");

        // Act
        var result = recipe.Update(new RecipeParams("", "Other"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(RecipeParams.Name));
        recipe.Name.Should().Be("Burger");
        recipe.Description.Should().Be("With fries");
        recipe.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void Hide_ShouldRaiseRecipeHiddenDomainEvent()
    {
        // Arrange
        var recipe = CreateRecipe();

        // Act
        recipe.Hide();

        // Assert
        recipe.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<RecipeHiddenDomainEvent>()
            .Which.RecipeId.Should().Be(recipe.Id);
    }
}
