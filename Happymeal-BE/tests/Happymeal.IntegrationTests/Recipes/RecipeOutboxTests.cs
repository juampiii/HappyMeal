using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;
using Happymeal.Infrastructure.Outbox;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipeOutboxTests(HappymealApiFactory factory) : TestBase(factory)
{
    private const string RecipesRoute = "api/recipes";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<List<OutboxMessage>> GetOutboxMessagesAsync()
        => QueryAsync(db => db.OutboxMessages.AsNoTracking().ToListAsync(CancellationToken));

    private async Task<List<OutboxMessage>> WaitForAllProcessedAsync(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var messages = await GetOutboxMessagesAsync();
            if (messages.All(m => m.ProcessedOn is not null) || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken);
        }
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldStageRecipeCreatedMessageInOutbox()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            RecipesRoute, new CreateRecipeRequestDto(null, "Burger", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var messages = await GetOutboxMessagesAsync();
        messages.Should().ContainSingle(m => m.Type == typeof(RecipeCreatedDomainEvent).FullName);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ShouldNotStageOutboxMessage()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            RecipesRoute, new CreateRecipeRequestDto(null, "", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var messages = await GetOutboxMessagesAsync();
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldDeliverMessageWithoutDeadLettering()
    {
        // Arrange
        await Client.PostAsJsonAsync(RecipesRoute, new CreateRecipeRequestDto(null, "Burger", null), CancellationToken);

        // Act
        var messages = await WaitForAllProcessedAsync();

        // Assert
        var message = messages.Should().ContainSingle().Subject;
        message.ProcessedOn.Should().NotBeNull();
        message.DeadLetteredOn.Should().BeNull();
        message.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_ShouldStageRecipeUpdatedMessageInOutbox()
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{RecipesRoute}/{recipe.Id}", new UpdateRecipeRequestDto("Burger XL", null), CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await GetOutboxMessagesAsync();
        messages.Should().ContainSingle(m => m.Type == typeof(RecipeUpdatedDomainEvent).FullName);
    }

    [Fact]
    public async Task DeleteAsync_WhenRecipeExists_ShouldStageRecipeDeletedMessageInOutbox()
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());

        // Act
        var response = await Client.DeleteAsync($"{RecipesRoute}/{recipe.Id}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var messages = await GetOutboxMessagesAsync();
        messages.Should().ContainSingle(m => m.Type == typeof(RecipeDeletedDomainEvent).FullName);
    }
}
