using Happymeal.Domain;

namespace Happymeal.UnitTests.Domain;

public class RecipeNameTests
{
    [Theory]
    [InlineData(" TÓRTILLA ", "tortilla")]
    // The first spelling uses a combining diaeresis; the second uses a composed ü.
    [InlineData("pingüino", "PINGÜINO")]
    public void Key_WhenNamesDifferOnlyByConfirmedRules_ShouldReturnTheSameKey(
        string firstName,
        string secondName)
    {
        // Act
        var firstKey = RecipeName.Key(firstName);
        var secondKey = RecipeName.Key(secondName);

        // Assert
        firstKey.Should().Be(secondKey);
    }
}
