using Happymeal.Application.Features.Recipes.DomainEventHandlers;
using Happymeal.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Happymeal.UnitTests.Application.Features.Recipes.DomainEventHandlers;

public class LogRecipeLifecycleDomainEventHandlerTests
{
    private readonly LogRecipeLifecycleDomainEventHandler _sut =
        new(Substitute.For<ILogger<LogRecipeLifecycleDomainEventHandler>>());

    [Fact]
    public async Task HandleAsync_WhenRecipeCreated_ShouldReturnSuccess()
    {
        var result = await _sut.HandleAsync(
            new RecipeCreatedDomainEvent(Guid.CreateVersion7(), "Burger"), Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeUpdated_ShouldReturnSuccess()
    {
        var result = await _sut.HandleAsync(
            new RecipeUpdatedDomainEvent(Guid.CreateVersion7(), "Burger XL"), Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeDeleted_ShouldReturnSuccess()
    {
        var result = await _sut.HandleAsync(
            new RecipeDeletedDomainEvent(Guid.CreateVersion7()), Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
