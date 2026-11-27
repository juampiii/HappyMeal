using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes;
using Happymeal.Domain;
using Happymeal.Domain.Params;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipesEndpointsTests(HappymealApiFactory factory) : TestBase(factory)
{
    private const string RecipesRoute = "api/recipes";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Recipe> SeedRecipeAsync(string name = "Burger", string? description = null)
    {
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams(name, description)).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());
        return recipe;
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldReturnCreatedAndPersistRecipe()
    {
        // Arrange
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Burger",
            Description: "With fries",
            Ingredients: [new("Potato", 200m, "g")],
            Steps: ["Cut", "Cook"],
            Servings: 2,
            PreparationMinutes: 20,
            Tags: ["quick"]);

        // Act
        var response = await Client.PostAsJsonAsync(RecipesRoute, request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        dto!.Name.Should().Be("Burger");
        response.Headers.Location!.ToString().Should().EndWith($"{RecipesRoute}/{dto.Id}");

        var stored = await QueryAsync(db => db.Recipes.AsNoTracking().SingleAsync(r => r.Id == dto.Id, CancellationToken));
        stored.Name.Should().Be("Burger");
        stored.Description.Should().Be("With fries");
        stored.Ingredients.Should().ContainSingle();
        var ingredient = stored.Ingredients.Single();
        ingredient.Name.Should().Be("Potato");
        ingredient.Quantity.Should().Be(200m);
        ingredient.Unit!.Code.Should().Be("g");
        dto.Ingredients!.Single().Unit.Should().Be("g");
        stored.Steps.Select(step => step.Content).Should().Equal("Cut", "Cook");
        stored.Servings.Should().Be(2);
        stored.PreparationMinutes.Should().Be(20);
        stored.Tags.Select(tag => tag.Name).Should().Equal("quick");
        stored.CreatedBy.Should().Be(ICurrentUser.Anonymous);
        stored.CreatedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        stored.ModifiedOn.Should().BeNull();
        stored.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenRequestHasId_ShouldUseThatId()
    {
        // Arrange
        var id = Guid.CreateVersion7();

        // Act
        var response = await Client.PostAsJsonAsync(
            RecipesRoute, new CreateRecipeRequestDto(id, "Burger", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        dto!.Id.Should().Be(id);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsEmpty_ShouldReturnBadRequestAndNotPersist()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            RecipesRoute, new CreateRecipeRequestDto(null, "", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        problem!.Errors.Should().ContainKey("name");

        var count = await QueryAsync(db => db.Recipes.CountAsync(CancellationToken));
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WhenIdAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existing = await SeedRecipeAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            RecipesRoute, new CreateRecipeRequestDto(existing.Id, "Other", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);

        var stored = await QueryAsync(db => db.Recipes.AsNoTracking().SingleAsync(CancellationToken));
        stored.Name.Should().Be("Burger");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecipeExists_ShouldReturnOk()
    {
        // Arrange
        var recipe = await SeedRecipeAsync("Burger", "With fries");

        // Act
        var response = await Client.GetAsync($"{RecipesRoute}/{recipe.Id}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        dto!.Name.Should().Be("Burger");
        dto.Description.Should().Be("With fries");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.GetAsync($"{RecipesRoute}/{Guid.CreateVersion7()}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem!.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsNotAGuid_ShouldReturnNotFound()
    {
        var response = await Client.GetAsync($"{RecipesRoute}/not-a-guid", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_WhenRecipesExist_ShouldReturnAllOfThem()
    {
        // Arrange
        var burger = await SeedRecipeAsync("Burger");
        var salad = await SeedRecipeAsync("Salad");

        // Act
        var response = await Client.GetAsync(RecipesRoute, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<RecipeDto>>(CancellationToken);
        dtos!.Select(d => d.Id).Should().BeEquivalentTo([burger.Id, salad.Id]);
    }

    [Fact]
    public async Task GetAllAsync_WhenThereAreNoRecipes_ShouldReturnEmptyList()
    {
        // Act
        var response = await Client.GetAsync(RecipesRoute, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<RecipeDto>>(CancellationToken);
        dtos.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_ShouldReturnOkAndPersistChanges()
    {
        // Arrange
        var recipe = await SeedRecipeAsync("Burger", "With fries");

        var request = new UpdateRecipeRequestDto(
            Name: "Burger XL",
            Description: "Big",
            Ingredients: [new("Bread", 2m, "ud")],
            Steps: ["Toast", "Assemble"],
            Servings: 3,
            PreparationMinutes: 25,
            Tags: ["homemade"]);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{RecipesRoute}/{recipe.Id}",
            request,
            CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken);
        dto!.Name.Should().Be("Burger XL");
        dto.Description.Should().Be("Big");

        var stored = await QueryAsync(db => db.Recipes.AsNoTracking().SingleAsync(CancellationToken));
        stored.Name.Should().Be("Burger XL");
        stored.Description.Should().Be("Big");
        stored.Id.Should().Be(recipe.Id);
        stored.Ingredients.Should().ContainSingle();
        var ingredient = stored.Ingredients.Single();
        ingredient.Name.Should().Be("Bread");
        ingredient.Quantity.Should().Be(2m);
        ingredient.Unit!.Code.Should().Be("ud");
        dto.Ingredients!.Single().Unit.Should().Be("ud");
        stored.Steps.Select(step => step.Content).Should().Equal("Toast", "Assemble");
        stored.Servings.Should().Be(3);
        stored.PreparationMinutes.Should().Be(25);
        stored.Tags.Select(tag => tag.Name).Should().Equal("homemade");
        stored.CreatedBy.Should().Be(ICurrentUser.System);
        stored.ModifiedBy.Should().Be(ICurrentUser.Anonymous);
        stored.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task UpdateAsync_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.PutAsJsonAsync(
            $"{RecipesRoute}/{Guid.CreateVersion7()}", new UpdateRecipeRequestDto("Burger", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameIsEmpty_ShouldReturnBadRequestAndKeepRecipe()
    {
        // Arrange
        var recipe = await SeedRecipeAsync("Burger");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{RecipesRoute}/{recipe.Id}", new UpdateRecipeRequestDto("", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        problem!.Errors.Should().ContainKey("name");

        var stored = await QueryAsync(db => db.Recipes.AsNoTracking().SingleAsync(CancellationToken));
        stored.Name.Should().Be("Burger");
    }

    [Fact]
    public async Task HideAsync_WhenRecipeExists_ShouldReturnNoContentAndPreserveRecipe()
    {
        // Arrange
        var recipe = await SeedRecipeAsync();

        // Act
        var response = await Client.DeleteAsync($"{RecipesRoute}/{recipe.Id}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await QueryAsync(db => db.Recipes.AnyAsync(r => r.Id == recipe.Id, CancellationToken));
        exists.Should().BeTrue();
        var hidden = await QueryAsync(db => db.Recipes.SingleAsync(r => r.Id == recipe.Id, CancellationToken));
        hidden.IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.DeleteAsync($"{RecipesRoute}/{Guid.CreateVersion7()}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
