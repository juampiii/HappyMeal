using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Features.Recipes;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.IntegrationTests.Recipes;

public class UnitOfMeasurePersistenceTests(HappymealApiFactory factory) : TestBase(factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(null, "g")]
    [InlineData(2, null)]
    [InlineData(2, "cucharada")]
    public async Task CreateAsync_WhenQuantityAndUnitAreIndependentlyOptional_ShouldRoundTripTheExistingContract(
        int? quantity,
        string? unitCode)
    {
        // Arrange
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Soup",
            Ingredients: [new("Salt", quantity, unitCode)]);

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        var stored = await QueryAsync(db => db.Recipes.AsNoTracking()
            .SingleAsync(recipe => recipe.Id == created!.Id, CancellationToken));
        var detail = await Client.GetFromJsonAsync<RecipeDto>(
            $"api/recipes/{created!.Id}",
            CancellationToken);

        var storedIngredient = stored.Ingredients.Single();
        var storedUnitCode = storedIngredient.Unit?.Code;
        var returnedIngredient = detail!.Ingredients!.Single();

        storedUnitCode.Should().Be(unitCode);
        storedIngredient.Quantity.Should().Be(quantity);
        returnedIngredient.Unit.Should().Be(unitCode);
        returnedIngredient.Quantity.Should().Be(quantity);
    }

    [Fact]
    public async Task Model_WhenUnitUsesAValueConverter_ShouldMatchTheExistingMigrationSnapshot()
    {
        // Act
        var hasPendingChanges = await QueryAsync(db => Task.FromResult(db.Database.HasPendingModelChanges()));

        // Assert
        hasPendingChanges.Should().BeFalse();
    }
}
