using Happymeal.Application.Common.Interfaces;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Params;
using Happymeal.Infrastructure.Database;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipePersistenceConflictTests(HappymealApiFactory factory) : TestBase(factory)
{
    [Theory]
    [InlineData(true, "Recipe id already exists.")]
    [InlineData(false, "Recipe name already exists.")]
    public async Task TrySaveEntitiesAsync_WhenDatabaseRejectsDuplicate_ShouldIdentifyConflictAndRollBack(
        bool duplicateId,
        string expectedMessage)
    {
        // Arrange: bypass prechecks so the database constraint determines the result.
        var cancellationToken = TestContext.Current.CancellationToken;
        var original = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Soup")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(original, cancellationToken).AsTask());
        var candidate = Recipe.Create(
            duplicateId ? original.Id : Guid.CreateVersion7(),
            new RecipeParams(duplicateId ? "Salad" : "SÓUP")).Value;
        using var scope = Factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<HappymealDbContext>();
        unitOfWork.RecipeRepository.Add(candidate);

        // Act
        var result = await unitOfWork.TrySaveEntitiesAsync(cancellationToken);

        // Assert
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Be(expectedMessage);
        dbContext.ChangeTracker.Entries().Should().BeEmpty();
        var recipeCount = await QueryAsync(db => db.Recipes.CountAsync(cancellationToken));
        var eventCount = await QueryAsync(db => db.OutboxMessages.CountAsync(cancellationToken));
        recipeCount.Should().Be(1);
        eventCount.Should().Be(0);
    }
}
