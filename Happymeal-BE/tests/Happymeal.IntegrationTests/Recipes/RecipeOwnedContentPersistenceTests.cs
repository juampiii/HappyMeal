using System.Net.Http.Json;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipeOwnedContentPersistenceTests(HappymealApiFactory factory) : TestBase(factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAsync_WhenStepsWereStoredOutOfOrder_ShouldReturnThemByPositionInDetailsAndListing()
    {
        // Arrange: insertion order deliberately differs from preparation order.
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Soup")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());
        await QueryAsync(db => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Happymeal"."RecipeSteps" ("RecipeId", "Position", "Content")
            VALUES ({recipe.Id}, 2, 'Serve'), ({recipe.Id}, 0, 'Chop'), ({recipe.Id}, 1, 'Cook')
            """, CancellationToken));

        // Act
        var stored = await QueryAsync(db => db.Recipes.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == recipe.Id, CancellationToken));
        var detail = await Client.GetFromJsonAsync<RecipeDto>($"api/recipes/{recipe.Id}", CancellationToken);
        var listing = await Client.GetFromJsonAsync<RecipeDto[]>("api/recipes", CancellationToken);

        // Assert
        stored.Steps.Select(step => step.Position).Should().Equal(0, 1, 2);
        stored.Steps.Select(step => step.Content).Should().Equal("Chop", "Cook", "Serve");
        detail!.Steps.Should().Equal("Chop", "Cook", "Serve");
        listing!.Single().Steps.Should().Equal("Chop", "Cook", "Serve");
    }

    [Fact]
    public async Task SaveAsync_WhenOwnedContentIsReplacedRepeatedly_ShouldPersistChangesWithAuditAndEvents()
    {
        // Arrange
        var recipe = await SeedCompleteDraftAsync();
        using var editingScope = Factory.Services.CreateScope();
        var unitOfWork = editingScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var draft = (await unitOfWork.RecipeRepository.GetAsync(recipe.Id, CancellationToken))!;
        var originalRevision = draft.Revision;

        // Act: reuse the same tracker and keys while changing, removing and adding elements.
        var firstEdit = draft.Update(new RecipeParams(
            Name: "Soup",
            Ingredients: [new("Water")],
            Steps: ["Boil", "Serve", "Enjoy"],
            Tags: ["quick", "hot"]));
        var firstSave = await unitOfWork.TrySaveEntitiesAsync(CancellationToken);
        var secondEdit = draft.Update(new RecipeParams(
            Name: "Soup",
            Ingredients: [new("Water")],
            Steps: ["Simmer"],
            Tags: ["hot"]));
        var secondSave = await unitOfWork.TrySaveEntitiesAsync(CancellationToken);

        // Assert
        firstEdit.IsSuccess.Should().BeTrue();
        firstSave.IsSuccess.Should().BeTrue();
        secondEdit.IsSuccess.Should().BeTrue();
        secondSave.IsSuccess.Should().BeTrue();
        var stored = await QueryAsync(db => db.Recipes.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == recipe.Id, CancellationToken));
        var updateEvents = await QueryAsync(db => db.OutboxMessages.CountAsync(
            message => message.Type == typeof(RecipeUpdatedDomainEvent).FullName,
            CancellationToken));

        stored.Steps.Select(step => step.Content).Should().Equal("Simmer");
        stored.Steps.Single().Position.Should().Be(0);
        stored.Tags.Select(tag => tag.Name).Should().Equal("hot");
        stored.Revision.Should().NotBe(originalRevision);
        stored.ModifiedOn.Should().NotBeNull();
        stored.ModifiedBy.Should().Be(ICurrentUser.System);
        updateEvents.Should().Be(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveAsync_WhenStepOrTagEditLosesToPublication_ShouldRollBackContentAndEvent(bool changeSteps)
    {
        // Arrange: neither edit changes root properties or ingredients.
        var recipe = await SeedCompleteDraftAsync();
        using var editingScope = Factory.Services.CreateScope();
        using var publishingScope = Factory.Services.CreateScope();
        var editingUnitOfWork = editingScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishingUnitOfWork = publishingScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var draftToEdit = (await editingUnitOfWork.RecipeRepository.GetAsync(recipe.Id, CancellationToken))!;
        var draftToPublish = (await publishingUnitOfWork.RecipeRepository.GetAsync(recipe.Id, CancellationToken))!;
        var editResult = draftToEdit.Update(new RecipeParams(
            Name: "Soup",
            Ingredients: [new("Water")],
            Steps: changeSteps ? ["Different preparation"] : ["Boil", "Serve"],
            Tags: changeSteps ? ["quick"] : ["hot"]));
        editResult.IsSuccess.Should().BeTrue();
        draftToPublish.Publish().IsSuccess.Should().BeTrue();
        var publicationSave = await publishingUnitOfWork.TrySaveEntitiesAsync(CancellationToken);
        publicationSave.IsSuccess.Should().BeTrue();

        // Act
        var staleEditSave = await editingUnitOfWork.TrySaveEntitiesAsync(CancellationToken);

        // Assert
        staleEditSave.Error!.Type.Should().Be(ErrorType.Conflict);
        var stored = await QueryAsync(db => db.Recipes.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == recipe.Id, CancellationToken));
        var updateEvents = await QueryAsync(db => db.OutboxMessages.CountAsync(
            message => message.Type == typeof(RecipeUpdatedDomainEvent).FullName,
            CancellationToken));

        stored.IsPublished.Should().BeTrue();
        stored.Steps.Select(step => step.Content).Should().Equal("Boil", "Serve");
        stored.Tags.Select(tag => tag.Name).Should().Equal("quick");
        updateEvents.Should().Be(0);
    }

    private async Task<Recipe> SeedCompleteDraftAsync()
    {
        var content = new RecipeParams(
            Name: "Soup",
            Ingredients: [new("Water")],
            Steps: ["Boil", "Serve"],
            Tags: ["quick"]);
        var recipe = Recipe.Create(Guid.CreateVersion7(), content).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());
        return recipe;
    }
}
