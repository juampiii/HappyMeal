using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain;

public class UnitOfMeasureTests
{
    [Theory]
    [InlineData("g")]
    [InlineData("kg")]
    [InlineData("ml")]
    [InlineData("l")]
    [InlineData("ud")]
    [InlineData("cucharadita")]
    [InlineData("cucharada")]
    public void Create_WhenCodeIsSupported_ShouldReturnThatUnit(string code)
    {
        // Act
        var result = UnitOfMeasure.Create(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    [InlineData("G")]
    [InlineData(" g ")]
    public void Create_WhenCodeIsNotSupported_ShouldReturnValidationFailure(string? code)
    {
        // Act
        var result = UnitOfMeasure.Create(code);

        // Assert
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(UnitOfMeasure.Code));
    }

    [Fact]
    public void Equality_WhenSeparateInstancesHaveTheSameCode_ShouldUseTheCodeValue()
    {
        // Arrange: copying a record preserves its getter-only code.
        var gram = UnitOfMeasure.Gram;
        var anotherGram = gram with { };

        // Act
        var distinctUnits = new HashSet<UnitOfMeasure> { gram, anotherGram, UnitOfMeasure.Kilogram };

        // Assert
        anotherGram.Should().NotBeSameAs(gram);
        anotherGram.Should().Be(gram);
        anotherGram.GetHashCode().Should().Be(gram.GetHashCode());
        UnitOfMeasure.Kilogram.Should().NotBe(gram);
        distinctUnits.Should().HaveCount(2);
    }

    [Fact]
    public void CreateRecipe_WhenUnitHasNoQuantity_ShouldKeepAnOptionalQuantity()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Ingredients: [new("Salt", Unit: "cucharadita")]);

        // Act
        var result = Recipe.Create(Guid.NewGuid(), parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ingredient = result.Value.Ingredients.Single();
        ingredient.Unit.Should().Be(UnitOfMeasure.Teaspoon);
        ingredient.Quantity.Should().BeNull();
    }

    [Fact]
    public void CreateRecipe_WhenQuantityHasNoUnit_ShouldKeepAnOptionalUnit()
    {
        // Arrange
        var parameters = new RecipeParams(
            Name: "Soup",
            Ingredients: [new("Salt", Quantity: 2m)]);

        // Act
        var result = Recipe.Create(Guid.NewGuid(), parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ingredient = result.Value.Ingredients.Single();
        ingredient.Unit.Should().BeNull();
        ingredient.Quantity.Should().Be(2m);
    }
}
