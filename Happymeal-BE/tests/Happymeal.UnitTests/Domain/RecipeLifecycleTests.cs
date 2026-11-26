using Happymeal.Domain;
using Happymeal.Domain.Params;
using Happymeal.Domain.Events;
using Happymeal.Common.Results;
namespace Happymeal.UnitTests.Domain;
public class RecipeLifecycleTests
{
    private static Recipe Complete() => Recipe.Create(Guid.NewGuid(), new RecipeParams("Tortilla", Ingredients: [new("Sal", 0.5m, "cucharada")], Steps: ["Mezclar", "Cocinar"])).Value;
    [Fact]
    public void Publish_WhenIncomplete_ShouldKeepDraftAndNotEmitEvent()
    {
        var recipe = Recipe.Create(Guid.NewGuid(), new RecipeParams("Tortilla")).Value;
        recipe.PullDomainEvents();
        recipe.Publish().Error!.Type.Should().Be(ErrorType.Validation);
        recipe.IsPublished.Should().BeFalse();
        recipe.GetDomainEvents().Should().BeEmpty();
    }
    [Fact]
    public void Publish_WhenRepeated_ShouldFreezeContentAndEmitOnlyOneEvent()
    {
        var recipe = Complete();
        recipe.PullDomainEvents();
        recipe.Publish().IsSuccess.Should().BeTrue();
        recipe.Publish().IsSuccess.Should().BeTrue();
        recipe.Update(new RecipeParams("Changed")).Error!.Type.Should().Be(ErrorType.Conflict);
        recipe.Name.Should().Be("Tortilla");
        recipe.GetDomainEvents().Should().ContainSingle().Which.Should().BeOfType<RecipePublishedDomainEvent>();
        recipe.Version.Should().Be(1);
    }
    [Fact]
    public void Hide_WhenRepeated_ShouldPreservePublishedContentAndEmitOnlyOneEvent()
    {
        var recipe = Complete();
        recipe.Publish();
        recipe.PullDomainEvents();
        recipe.Hide();
        recipe.Hide();
        recipe.IsHidden.Should().BeTrue();
        recipe.IsPublished.Should().BeTrue();
        recipe.Ingredients.Should().ContainSingle();
        recipe.Steps.Should().Equal("Mezclar", "Cocinar");
        recipe.GetDomainEvents().Should().ContainSingle().Which.Should().BeOfType<RecipeHiddenDomainEvent>();
    }
    [Fact]
    public void Update_WhenIngredientsRepeat_ShouldKeepContentAndEventsUnchanged()
    {
        var recipe = Complete();
        recipe.PullDomainEvents();
        recipe.Update(new RecipeParams("Other", Ingredients: [new(" SAL "), new("s\u00e1l")], Steps: ["Other"])).IsFailure.Should().BeTrue();
        recipe.Name.Should().Be("Tortilla");
        recipe.Ingredients.Single().Quantity.Should().Be(0.5m);
        recipe.Steps.Should().Equal("Mezclar", "Cocinar");
        recipe.GetDomainEvents().Should().BeEmpty();
    }
    [Theory]
    [InlineData(" T\u00d3RTILLA ", "tortilla")]
    [InlineData("pingu\u0308ino", "PING\u00dcINO")]
    public void Key_WhenNamesDifferOnlyByConfirmedRules_ShouldCompareEqual(string first,string second)
        => RecipeName.Key(first).Should().Be(RecipeName.Key(second));
    [Theory]
    [InlineData("g", 0, false)]
    [InlineData("kg", -1, false)]
    [InlineData("unknown", 1, false)]
    [InlineData("cucharada", 1.5, true)]
    [InlineData(null, 1, true)]
    public void Validate_WhenQuantityOrUnitIsProvided_ShouldApplyConfirmedRules(string? unit, double quantity, bool valid)
    {
        var parameters = new RecipeParams("Soup", Ingredients: [new("Salt", (decimal)quantity, unit)]);
        parameters.Validate().Any().Should().Be(!valid);
    }

    [Fact]
    public void Validate_WhenServingsOrMinutesAreNotPositive_ShouldReportBothFields()
    {
        var parameters = new RecipeParams("Soup", Servings: 0, PreparationMinutes: -1);
        parameters.Validate().Select(x => x.PropertyName).Should().BeEquivalentTo(["Servings", "PreparationMinutes"]);
    }
}
