using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Domain.Params;

public class RecipeParamsTests
{
    private static RecipeParams ValidParams() => new("Burger", "With fries");

    [Fact]
    public void Validate_WhenParamsAreValid_ShouldReturnNoFailures()
        => ValidParams().Validate().Should().BeEmpty();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmptyOrWhitespace_ShouldReturnNameFailure(string name)
        => (ValidParams() with { Name = name }).Validate()
            .Should().ContainSingle(f => f.PropertyName == nameof(RecipeParams.Name));

    [Fact]
    public void Validate_WhenNameIsLongerThanMaxLength_ShouldReturnNameFailure()
        => (ValidParams() with { Name = new string('a', Recipe.NameMaxLength + 1) }).Validate()
            .Should().ContainSingle(f => f.PropertyName == nameof(RecipeParams.Name));

    [Fact]
    public void Validate_WhenNameHasMaxLength_ShouldReturnNoFailures()
        => (ValidParams() with { Name = new string('a', Recipe.NameMaxLength) }).Validate()
            .Should().BeEmpty();

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldReturnNoFailures()
        => (ValidParams() with { Description = null }).Validate().Should().BeEmpty();

    [Fact]
    public void Validate_WhenDescriptionIsLongerThanMaxLength_ShouldReturnDescriptionFailure()
        => (ValidParams() with { Description = new string('a', Recipe.DescriptionMaxLength + 1) }).Validate()
            .Should().ContainSingle(f => f.PropertyName == nameof(RecipeParams.Description));

    [Fact]
    public void Validate_WhenDescriptionHasMaxLength_ShouldReturnNoFailures()
        => (ValidParams() with { Description = new string('a', Recipe.DescriptionMaxLength) }).Validate()
            .Should().BeEmpty();

    [Fact]
    public void Validate_WhenSeveralFieldsAreInvalid_ShouldReturnAllFailures()
        => new RecipeParams("", new string('a', Recipe.DescriptionMaxLength + 1)).Validate()
            .Select(f => f.PropertyName)
            .Should().BeEquivalentTo([nameof(RecipeParams.Name), nameof(RecipeParams.Description)]);
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenIngredientQuantityIsNotPositive_ShouldReturnQuantityFailure(int quantity)
    {
        // Arrange
        var parameters = ValidParams() with
        {
            Ingredients = [new("Salt", quantity, "g")]
        };

        // Act
        var failures = parameters.Validate().ToList();

        // Assert
        failures.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Ingredients[0].Quantity");
    }

    [Fact]
    public void Validate_WhenIngredientUnitIsUnknown_ShouldReturnUnitFailure()
    {
        // Arrange
        var parameters = ValidParams() with
        {
            Ingredients = [new("Salt", 1m, "unknown")]
        };

        // Act
        var failures = parameters.Validate().ToList();

        // Assert
        failures.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Ingredients[0].Unit");
    }

    [Theory]
    [InlineData("cucharada")]
    [InlineData(null)]
    public void Validate_WhenQuantityIsPositiveAndUnitIsSupportedOrOmitted_ShouldReturnNoFailures(string? unit)
    {
        // Arrange
        var parameters = ValidParams() with
        {
            Ingredients = [new("Salt", 1.5m, unit)]
        };

        // Act
        var failures = parameters.Validate();

        // Assert
        failures.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenServingsAndMinutesAreNotPositive_ShouldReturnBothFailures()
    {
        // Arrange
        var parameters = ValidParams() with
        {
            Servings = 0,
            PreparationMinutes = -1
        };

        // Act
        var failurePaths = parameters.Validate().Select(failure => failure.PropertyName);

        // Assert
        failurePaths.Should().BeEquivalentTo(["Servings", "PreparationMinutes"]);
    }
}
