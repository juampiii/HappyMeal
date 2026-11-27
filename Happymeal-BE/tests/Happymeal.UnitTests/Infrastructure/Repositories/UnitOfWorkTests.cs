using Happymeal.Application.Common.Interfaces;
using Happymeal.Infrastructure.Database;
using Happymeal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Happymeal.UnitTests.Infrastructure.Repositories;

public class UnitOfWorkTests
{
    [Fact]
    public async Task TrySaveEntitiesAsync_WhenUnrelatedUniqueConstraintFails_ShouldPropagateFailure()
    {
        // Arrange: an unrelated constraint must not be reported as a recipe name conflict.
        var postgresError = new PostgresException(
            messageText: "Duplicate tag",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            schemaName: HappymealDbContext.Schema,
            tableName: "RecipeTags",
            constraintName: "PK_RecipeTags");
        var databaseError = new DbUpdateException("Save failed", postgresError);
        var options = new DbContextOptionsBuilder<HappymealDbContext>()
            .UseNpgsql("Host=unused;Database=unused;Username=unused")
            .Options;
        await using var dbContext = new FailingSaveContext(options, databaseError);
        var unitOfWork = new UnitOfWork(dbContext, TimeProvider.System, Substitute.For<IRecipeRepository>());

        // Act
        var save = async () => await unitOfWork.TrySaveEntitiesAsync(TestContext.Current.CancellationToken);

        // Assert
        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.Which.Should().BeSameAs(databaseError);
    }

    private sealed class FailingSaveContext : HappymealDbContext
    {
        private readonly DbUpdateException _failure;

        public FailingSaveContext(
            DbContextOptions<HappymealDbContext> options,
            DbUpdateException failure) : base(options)
        {
            _failure = failure;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<int>(_failure);
        }
    }
}
