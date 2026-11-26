using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain;

public class RecipeLifecycleTests
{
    [Fact]
    public void Publish_WhenDraftIsIncomplete_ShouldKeepDraftAndNotRaiseEvents()
    {
        // Arrange
        var recipe = Recipe.Create(Guid.NewGuid(), new RecipeParams("Tortilla")).Value;
        recipe.PullDomainEvents();

        // Act
        var result = recipe.Publish();

        // Assert
        result.Error!.Type.Should().Be(ErrorType.Validation);
        recipe.IsPublished.Should().BeFalse();
        recipe.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void Publish_WhenCalledTwice_ShouldRaiseOnlyOnePublicationEvent()
    {
        // Arrange
        var recipe = CreateCompleteDraft();
        recipe.PullDomainEvents();

        // Act
        var firstPublication = recipe.Publish();
        var repeatedPublication = recipe.Publish();

        // Assert
        firstPublication.IsSuccess.Should().BeTrue();
        repeatedPublication.IsSuccess.Should().BeTrue();
        recipe.IsPublished.Should().BeTrue();
        recipe.Version.Should().Be(1);
        recipe.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<RecipePublishedDomainEvent>();
    }

    [Fact]
    public void Update_WhenRecipeIsPublished_ShouldRejectChangesAndKeepContent()
    {
        // Arrange
        var recipe = CreateCompleteDraft();
        recipe.Publish();
        recipe.PullDomainEvents();
        var changedContent = new RecipeParams("Changed recipe");

        // Act
        var result = recipe.Update(changedContent);

        // Assert
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        recipe.Name.Should().Be("Tortilla");
        recipe.Ingredients.Single().Name.Should().Be("Sal");
        recipe.Steps.Should().Equal("Mezclar", "Cocinar");
        recipe.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void Hide_WhenCalledTwice_ShouldKeepPublishedContentAndRaiseOnlyOneEvent()
    {
        // Arrange
        var recipe = CreateCompleteDraft();
        recipe.Publish();
        recipe.PullDomainEvents();

        // Act
        var firstHide = recipe.Hide();
        var repeatedHide = recipe.Hide();

        // Assert
        firstHide.IsSuccess.Should().BeTrue();
        repeatedHide.IsSuccess.Should().BeTrue();
        recipe.IsHidden.Should().BeTrue();
        recipe.IsPublished.Should().BeTrue();
        recipe.Ingredients.Single().Quantity.Should().Be(0.5m);
        recipe.Steps.Should().Equal("Mezclar", "Cocinar");
        recipe.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<RecipeHiddenDomainEvent>();
    }

    [Fact]
    public void Update_WhenIngredientsRepeat_ShouldKeepContentAndNotRaiseEvents()
    {
        // Arrange
        var recipe = CreateCompleteDraft();
        recipe.PullDomainEvents();
        var repeatedIngredientContent = new RecipeParams(
            Name: "Other recipe",
            Ingredients: [new(" SAL "), new("sál")],
            Steps: ["Different preparation"]);

        // Act
        var result = recipe.Update(repeatedIngredientContent);

        // Assert
        result.IsFailure.Should().BeTrue();
        recipe.Name.Should().Be("Tortilla");
        recipe.Ingredients.Single().Quantity.Should().Be(0.5m);
        recipe.Steps.Should().Equal("Mezclar", "Cocinar");
        recipe.GetDomainEvents().Should().BeEmpty();
    }

    private static Recipe CreateCompleteDraft()
    {
        var content = new RecipeParams(
            Name: "Tortilla",
            Ingredients: [new("Sal", 0.5m, "cucharada")],
            Steps: ["Mezclar", "Cocinar"]);

        return Recipe.Create(Guid.NewGuid(), content).Value;
    }
}
