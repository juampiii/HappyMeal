using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain;

public class IngredientNameTests
{
    [Theory]
    [InlineData(" TÓRTILLA ", "tortilla")]
    // The first spelling uses a combining diaeresis; the second uses a composed ü.
    [InlineData("pingüino", "PINGÜINO")]
    public void Create_WhenIngredientNamesDifferOnlyByConfirmedRules_ShouldRejectDuplicates(
        string firstName,
        string secondName)
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Ingredients: [new(firstName), new(secondName)]);

        // Act
        var result = Recipe.Create(Guid.CreateVersion7(), parameters);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.ValidationErrors.Should().ContainKey("Ingredients[1].Name");
    }
}
