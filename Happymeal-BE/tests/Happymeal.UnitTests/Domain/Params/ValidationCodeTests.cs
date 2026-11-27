using Happymeal.Common.Validation;
using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain.Params;

public class ValidationCodeTests
{
    [Fact]
    public void ToValidationError_WhenFieldHasMultipleFailures_ShouldPreserveCodesAndParameters()
    {
        // Arrange
        ValidationFailure[] failures = [
            new("Name", "First message", ValidationErrorCodes.Required),
            new("Name", "Second message", ValidationErrorCodes.MaxLength,
                new Dictionary<string, object> { ["max"] = 200 })
        ];

        // Act
        var error = failures.ToValidationError();

        // Assert
        error!.Code.Should().Be("validation.failed");
        error.ValidationErrors["Name"].Should().Equal("First message", "Second message");
        error.ValidationFailures.Select(failure => failure.Code)
            .Should().Equal("validation.required", "validation.maxLength");
        error.ValidationFailures[1].Parameters!["max"].Should().Be(200);
    }

    [Fact]
    public void Validate_WhenUnitIsUnsupported_ShouldKeepCodeWhileMappingIngredientPath()
    {
        // Arrange
        var parameters = new RecipeParams("Soup", Ingredients: [new("Salt", Unit: "invalid")]);

        // Act
        var error = parameters.Validate().ToValidationError();

        // Assert
        error!.ValidationFailures.Should().ContainSingle();
        error.ValidationFailures[0].PropertyName.Should().Be("Ingredients[0].Unit");
        error.ValidationFailures[0].Code.Should().Be("validation.unsupportedValue");
    }
}
