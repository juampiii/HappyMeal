using Happymeal.Domain.Params;
using Happymeal.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Happymeal.IntegrationTests.Recipes;

public class RecipeMigrationTests
{
    private const string InitialMigration = "20261031120108_InitialCreate";

    [Fact]
    public async Task MigrateAsync_WhenFeatureIsAppliedAndRemoved_ShouldPreserveBaseRecipe()
    {
        // Arrange: use an isolated database because this test changes its schema.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<HappymealDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var db = new HappymealDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration, cancellationToken);
        var recipeId = Guid.CreateVersion7();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Happymeal"."Recipes"
                ("Id", "Name", "Description", "CreatedOn", "CreatedBy")
            VALUES ({recipeId}, 'Soup', 'Original', {DateTimeOffset.UnixEpoch}, 'legacy')
            """, cancellationToken);

        // Act: upgrade directly from the original schema and persist owned content.
        await migrator.MigrateAsync(cancellationToken: cancellationToken);
        var recipe = await db.Recipes.SingleAsync(candidate => candidate.Id == recipeId, cancellationToken);
        var edit = recipe.Update(new RecipeParams(
            Name: "Soup",
            Description: "Original",
            Ingredients: [new("Water")],
            Steps: ["Boil", "Serve"],
            Tags: ["quick"]));
        edit.IsSuccess.Should().BeTrue();
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        // Assert: the final schema has the expected model and owned collections.
        var stored = await db.Recipes.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == recipeId, cancellationToken);
        stored.Version.Should().Be(1);
        stored.Ingredients.Single().Name.Should().Be("Water");
        stored.Steps.Select(step => step.Content).Should().Equal("Boil", "Serve");
        stored.Tags.Select(tag => tag.Name).Should().Equal("quick");
        db.Database.HasPendingModelChanges().Should().BeFalse();
        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        applied.Should().HaveCount(2);

        // Downgrade removes feature content but preserves the pre-feature recipe.
        await migrator.MigrateAsync(InitialMigration, cancellationToken);
        await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT "Name", "Description", "CreatedBy"
            FROM "Happymeal"."Recipes" WHERE "Id" = @id
            """, connection);
        command.Parameters.AddWithValue("id", recipeId);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            var hasRow = await reader.ReadAsync(cancellationToken);
            hasRow.Should().BeTrue();
            reader.GetString(0).Should().Be("Soup");
            reader.GetString(1).Should().Be("Original");
            reader.GetString(2).Should().Be("legacy");
        }

        // The collation must be removed after its column dependency, allowing reapply.
        await migrator.MigrateAsync(cancellationToken: cancellationToken);
        var reapplied = await db.Recipes.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == recipeId, cancellationToken);
        reapplied.Steps.Should().BeEmpty();
        reapplied.Version.Should().Be(1);
        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_WhenLegacyNamesExist_ShouldPreserveDataOrRejectCollision(bool collision)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<HappymealDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var db = new HappymealDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration, cancellationToken);
        string[] names = collision ? [" SÓPA ", "sopa"] : [" TÓRTILLA ", "pingüino", "Sopa de ajo"];
        foreach (var name in names)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Happymeal"."Recipes"
                    ("Id", "Name", "Description", "CreatedOn", "CreatedBy")
                VALUES ({Guid.CreateVersion7()}, {name}, 'Original', {DateTimeOffset.UnixEpoch}, 'legacy')
                """, cancellationToken);
        }

        // Act / Assert: a collision rolls back the complete migration.
        if (collision)
        {
            var migrate = async () => await migrator.MigrateAsync(cancellationToken: cancellationToken);
            var failure = await migrate.Should().ThrowAsync<PostgresException>();
            failure.Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
            applied.Should().Equal(InitialMigration);
            return;
        }

        await migrator.MigrateAsync(cancellationToken: cancellationToken);
        var recipes = await db.Recipes.AsNoTracking().ToListAsync(cancellationToken);
        recipes.Should().HaveCount(names.Length);
        foreach (var recipe in recipes)
        {
            names.Select(name => name.Trim()).Should().Contain(recipe.Name);
            recipe.Version.Should().Be(1);
            recipe.IsPublished.Should().BeFalse();
            recipe.IsHidden.Should().BeFalse();
            recipe.Ingredients.Should().BeEmpty();
            recipe.Steps.Should().BeEmpty();
            recipe.Tags.Should().BeEmpty();
            recipe.CreatedBy.Should().Be("legacy");
            recipe.CreatedOn.Should().Be(DateTimeOffset.UnixEpoch);
            recipe.Description.Should().Be("Original");
        }
    }
}
