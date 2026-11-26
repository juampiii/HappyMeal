using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes;
using Happymeal.Common.Results;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipeLifecycleTests(HappymealApiFactory factory) : TestBase(factory)
{
    private const string RecipesRoute = "api/recipes";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UpdateAsync_WhenDraftContentChanges_ShouldPersistIngredientsAndStepOrder()
    {
        // Arrange
        var draft = await CreateCompleteDraftAsync();
        var changedContent = new UpdateRecipeRequestDto(
            Name: "Tortilla",
            Ingredients: [new("Sal al gusto")],
            Steps: ["Cocinar", "Servir"]);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{RecipesRoute}/{draft.Id}",
            changedContent,
            CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var savedRecipe = await Client.GetFromJsonAsync<RecipeDto>(
            $"{RecipesRoute}/{draft.Id}",
            CancellationToken);

        savedRecipe!.Ingredients.Should().ContainSingle();
        var ingredient = savedRecipe.Ingredients!.Single();
        ingredient.Name.Should().Be("Sal al gusto");
        ingredient.Quantity.Should().BeNull();
        ingredient.Unit.Should().BeNull();
        savedRecipe.Steps.Should().Equal("Cocinar", "Servir");
    }

    [Fact]
    public async Task PublishAsync_WhenCalledTwice_ShouldPersistPublicationAndOnlyOneEvent()
    {
        // Arrange
        var draft = await CreateCompleteDraftAsync();
        var publicationRoute = $"{RecipesRoute}/{draft.Id}/publish";

        // Act
        var firstResponse = await Client.PostAsync(publicationRoute, null, CancellationToken);
        var repeatedResponse = await Client.PostAsync(publicationRoute, null, CancellationToken);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        repeatedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var savedRecipe = await Client.GetFromJsonAsync<RecipeDto>(
            $"{RecipesRoute}/{draft.Id}",
            CancellationToken);
        var publicationEvents = await QueryAsync(db => db.OutboxMessages.CountAsync(
            message => message.Type == typeof(RecipePublishedDomainEvent).FullName,
            CancellationToken));

        savedRecipe!.IsPublished.Should().BeTrue();
        savedRecipe.Version.Should().Be(1);
        publicationEvents.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WhenRecipeIsPublished_ShouldReturnConflictAndKeepItsName()
    {
        // Arrange
        var draft = await CreateCompleteDraftAsync();
        var publication = await Client.PostAsync(
            $"{RecipesRoute}/{draft.Id}/publish",
            null,
            CancellationToken);
        publication.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var changedContent = new UpdateRecipeRequestDto(Name: "Other recipe");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{RecipesRoute}/{draft.Id}",
            changedContent,
            CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var savedRecipe = await Client.GetFromJsonAsync<RecipeDto>(
            $"{RecipesRoute}/{draft.Id}",
            CancellationToken);
        savedRecipe!.Name.Should().Be("Tortilla");
    }

    [Fact]
    public async Task HideAsync_WhenCalledTwice_ShouldHideFromListingAndKeepPublishedDetails()
    {
        // Arrange
        var draft = await CreateCompleteDraftAsync();
        var recipeRoute = $"{RecipesRoute}/{draft.Id}";
        var publication = await Client.PostAsync($"{recipeRoute}/publish", null, CancellationToken);
        publication.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var firstResponse = await Client.DeleteAsync(recipeRoute, CancellationToken);
        var repeatedResponse = await Client.DeleteAsync(recipeRoute, CancellationToken);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        repeatedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var hiddenRecipe = await Client.GetFromJsonAsync<RecipeDto>(recipeRoute, CancellationToken);
        var visibleRecipes = await Client.GetFromJsonAsync<RecipeDto[]>(RecipesRoute, CancellationToken);
        var hiddenEvents = await QueryAsync(db => db.OutboxMessages.CountAsync(
            message => message.Type == typeof(RecipeHiddenDomainEvent).FullName,
            CancellationToken));

        hiddenRecipe!.IsHidden.Should().BeTrue();
        hiddenRecipe.IsPublished.Should().BeTrue();
        hiddenRecipe.Ingredients!.Select(ingredient => ingredient.Name).Should().Equal("Sal", "Huevos");
        hiddenRecipe.Steps.Should().Equal("Mezclar", "Cocinar");
        visibleRecipes.Should().BeEmpty();
        hiddenEvents.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WhenEquivalentNamesRace_ShouldPersistOnlyOneRecipe()
    {
        // Arrange
        var accentedName = new CreateRecipeRequestDto(Id: null, Name: " SÓPA ");
        var equivalentName = new CreateRecipeRequestDto(Id: null, Name: "sopa");

        // Act
        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync(RecipesRoute, accentedName, CancellationToken),
            Client.PostAsJsonAsync(RecipesRoute, equivalentName, CancellationToken));

        // Assert
        var responseStatuses = responses.Select(response => response.StatusCode);
        var savedRecipeCount = await QueryAsync(db => db.Recipes.CountAsync(CancellationToken));

        responseStatuses.Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
        savedRecipeCount.Should().Be(1);
    }

    [Theory]
    [InlineData("Tortilla")]
    [InlineData("Changed recipe")]
    public async Task SaveAsync_WhenDraftEditLosesToPublication_ShouldRollBackContentAndUpdateEvent(string editedName)
    {
        // Arrange: two requests load the same draft before either request saves.
        // Keeping the same name covers an edit that changes only ingredients.
        var draft = await CreateCompleteDraftAsync();
        using var editingScope = Factory.Services.CreateScope();
        using var publishingScope = Factory.Services.CreateScope();
        var editingUnitOfWork = editingScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishingUnitOfWork = publishingScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var recipeToEdit = (await editingUnitOfWork.RecipeRepository.GetAsync(draft.Id, CancellationToken))!;
        var recipeToPublish = (await publishingUnitOfWork.RecipeRepository.GetAsync(draft.Id, CancellationToken))!;
        var editedContent = new RecipeParams(
            Name: editedName,
            Ingredients: [new("Changed ingredient")],
            Steps: ["Mezclar", "Cocinar"]);

        recipeToEdit.Update(editedContent).IsSuccess.Should().BeTrue();
        recipeToPublish.Publish().IsSuccess.Should().BeTrue();
        var publicationSave = await publishingUnitOfWork.TrySaveEntitiesAsync(CancellationToken);
        publicationSave.IsSuccess.Should().BeTrue();

        // Act: the stale edit attempts to save after publication has committed.
        var staleEditSave = await editingUnitOfWork.TrySaveEntitiesAsync(CancellationToken);

        // Assert: neither the stale content nor its event was committed.
        staleEditSave.Error!.Type.Should().Be(ErrorType.Conflict);
        var savedRecipe = await Client.GetFromJsonAsync<RecipeDto>(
            $"{RecipesRoute}/{draft.Id}",
            CancellationToken);
        var updateEvents = await QueryAsync(db => db.OutboxMessages.CountAsync(
            message => message.Type == typeof(RecipeUpdatedDomainEvent).FullName,
            CancellationToken));

        savedRecipe!.Name.Should().Be("Tortilla");
        savedRecipe.IsPublished.Should().BeTrue();
        savedRecipe.Ingredients!.Select(ingredient => ingredient.Name).Should().Equal("Sal", "Huevos");
        savedRecipe.Steps.Should().Equal("Mezclar", "Cocinar");
        updateEvents.Should().Be(0);
    }

    private async Task<RecipeDto> CreateCompleteDraftAsync()
    {
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Tortilla",
            Ingredients: [new("Sal", 0.5m, "cucharada"), new("Huevos", 2, "ud")],
            Steps: ["Mezclar", "Cocinar"]);
        var response = await Client.PostAsJsonAsync(RecipesRoute, request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RecipeDto>(CancellationToken))!;
    }
}
