using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Features.Recipes;
using Happymeal.Domain;
using Happymeal.Domain.Params;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipeReviewCorrectionsTests(HappymealApiFactory factory) : TestBase(factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateAsync_WhenPublishedRecipeWasHidden_ShouldReuseNameAndKeepOriginalAccessible()
    {
        // Arrange
        var original = Recipe.Create(Guid.CreateVersion7(), new RecipeParams(
            Name: "TÓRTILLA",
            Ingredients: [new("Egg")],
            Steps: ["Cook"])).Value;
        original.Publish();
        await SeedAsync(db => db.Recipes.AddAsync(original, CancellationToken).AsTask());
        var hideResponse = await Client.DeleteAsync($"api/recipes/{original.Id}", CancellationToken);
        hideResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var createResponse = await Client.PostAsJsonAsync(
            "api/recipes",
            new CreateRecipeRequestDto(Id: null, Name: " tortilla "),
            CancellationToken);

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var replacement = await createResponse.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        var oldDetails = await Client.GetFromJsonAsync<RecipeDto>($"api/recipes/{original.Id}", CancellationToken);
        var listing = await Client.GetFromJsonAsync<RecipeDto[]>("api/recipes", CancellationToken);
        replacement!.Id.Should().NotBe(original.Id);
        oldDetails!.IsHidden.Should().BeTrue();
        oldDetails.IsPublished.Should().BeTrue();
        listing!.Select(recipe => recipe.Id).Should().Equal(replacement.Id);
    }

    [Fact]
    public async Task SaveAsync_WhenHiddenNameIsReusedWithoutPrecheck_ShouldAllowOnlyOneVisibleRecipe()
    {
        // Arrange
        var hidden = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("SÓPA")).Value;
        hidden.Hide();
        await SeedAsync(db => db.Recipes.AddAsync(hidden, CancellationToken).AsTask());
        var visible = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("sopa")).Value;

        // Act: bypass the repository name precheck to exercise the partial index.
        await SeedAsync(db => db.Recipes.AddAsync(visible, CancellationToken).AsTask());

        // Assert
        var storedCount = await QueryAsync(db => db.Recipes.CountAsync(CancellationToken));
        storedCount.Should().Be(2);
        var conflict = await Client.PostAsJsonAsync(
            "api/recipes",
            new CreateRecipeRequestDto(Id: null, Name: "SOPA"),
            CancellationToken);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateAsync_WhenContentExceedsLimits_ShouldReturnFieldErrorsWithoutSaving()
    {
        // Arrange: the tag is deliberately larger than a PostgreSQL index entry.
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Soup",
            Ingredients: [new(new string('i', RecipeIngredient.NameMaxLength + 1))],
            Steps: [new string('s', RecipeStep.ContentMaxLength + 1)],
            Tags: [new string('t', 4000)]);

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        problem!.Errors.Keys.Should().BeEquivalentTo("ingredients[0].name", "steps[0]", "tags[0]");
        var recipeCount = await QueryAsync(db => db.Recipes.CountAsync(CancellationToken));
        recipeCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WhenRawTagCountExceedsLimit_ShouldRejectBeforeDiscardingBlanks()
    {
        // Arrange
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Soup",
            Tags: [.. Enumerable.Repeat("", Recipe.TagsMaxInputCount + 1)]);

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        problem!.Errors.Keys.Should().Equal("tags");
        var recipeCount = await QueryAsync(db => db.Recipes.CountAsync(CancellationToken));
        recipeCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WhenTagsContainCaseVariantsAndBlanks_ShouldPersistOneTrimmedTag()
    {
        // Arrange
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Soup",
            Tags: [" Rápida ", "rápida", "RÁPIDA", "", " "]);

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        var saved = await Client.GetFromJsonAsync<RecipeDto>($"api/recipes/{created!.Id}", CancellationToken);
        saved!.Tags.Should().Equal("Rápida");
    }
}
