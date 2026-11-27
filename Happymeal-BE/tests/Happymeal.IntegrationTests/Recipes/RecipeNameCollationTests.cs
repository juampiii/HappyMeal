using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Domain;
using Happymeal.Domain.Params;
using Happymeal.Infrastructure.Database;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipeNameCollationTests(HappymealApiFactory factory) : TestBase(factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("TÓRTILLA", " tortilla ")]
    [InlineData("pingüino", "PINGÜINO")]
    public async Task CreateAsync_WhenNameIsEquivalent_ShouldRejectDuplicate(
        string existingName,
        string requestedName)
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams(existingName)).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());

        // Act
        var response = await Client.PostAsJsonAsync(
            "api/recipes",
            new CreateRecipeRequestDto(Id: null, Name: requestedName),
            CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var recipeCount = await QueryAsync(db => db.Recipes.CountAsync(CancellationToken));
        recipeCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameIsEquivalentToAnotherRecipe_ShouldRejectDuplicate()
    {
        // Arrange
        var soup = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("SÓPA")).Value;
        var salad = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Salad")).Value;
        await SeedAsync(db =>
        {
            db.Recipes.AddRange(soup, salad);
            return Task.CompletedTask;
        });

        // Act
        var response = await Client.PutAsJsonAsync(
            $"api/recipes/{salad.Id}",
            new UpdateRecipeRequestDto(Name: " sopa "),
            CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var storedName = await QueryAsync(db => db.Recipes
            .Where(recipe => recipe.Id == salad.Id)
            .Select(recipe => recipe.Name)
            .SingleAsync(CancellationToken));
        storedName.Should().Be("Salad");
    }

    [Fact]
    public async Task SaveAsync_WhenPrecheckIsBypassed_ShouldEnforceUniqueNameInDatabase()
    {
        // Arrange
        var original = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("SÓPA")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(original, CancellationToken).AsTask());
        var duplicate = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("sopa")).Value;

        // Act
        var saveDuplicate = async () => await SeedAsync(db =>
            db.Recipes.AddAsync(duplicate, CancellationToken).AsTask());

        // Assert
        var failure = await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
        failure.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task NameExistsAsync_WhenExcludingCurrentRecipe_ShouldAllowSameNameAndSupportLike()
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("SÓPA")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();

        // Act
        var ownNameExists = await repository.NameExistsAsync("sopa", recipe.Id, CancellationToken);
        var duplicateExists = await repository.NameExistsAsync("sopa", Guid.Empty, CancellationToken);
        var matches = await QueryAsync(db => db.Recipes
            .Where(candidate => EF.Functions.Like(candidate.Name, "so%"))
            .Select(candidate => candidate.Name)
            .ToListAsync(CancellationToken));

        // Assert
        ownNameExists.Should().BeFalse();
        duplicateExists.Should().BeTrue();
        matches.Should().Equal("SÓPA");
    }
}
