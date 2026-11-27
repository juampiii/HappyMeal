using Happymeal.Domain;
using Happymeal.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
namespace Happymeal.IntegrationTests.Recipes;

public class RecipeMigrationTests
{
    [Fact]
    public async Task MigrateAsync_WhenRecipesContainStepsAndTags_ShouldPreserveContentOnUpgradeAndDowngrade()
    {
        // Arrange: use an isolated database because this test changes the schema version.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<HappymealDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var db = new HappymealDbContext(options);
        var migrator = db.GetService<IMigrator>();
        const string previousMigration = "20261126145551_RecipeLifecycle";
        await migrator.MigrateAsync(previousMigration, cancellationToken);
        var recipeId = Guid.CreateVersion7();
        var emptyDraftId = Guid.CreateVersion7();
        string[] steps = ["Chop", "Cook", "Serve"];
        string[] tags = ["quick", "vegan"];
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Happymeal"."Recipes"
                ("Id", "Name", "NameKey", "Steps", "Tags", "CreatedOn", "CreatedBy")
            VALUES ({recipeId}, 'Soup', 'SOUP', {steps}, {tags}, {DateTimeOffset.UnixEpoch}, 'legacy'),
                ({emptyDraftId}, 'Draft', 'DRAFT', ARRAY[]::text[], ARRAY[]::text[], {DateTimeOffset.UnixEpoch}, 'legacy')
            """, cancellationToken);

        // Act
        await migrator.MigrateAsync(cancellationToken: cancellationToken);

        // Assert: owned rows preserve step order and tag names, including empty drafts.
        var stored = await db.Recipes.AsNoTracking()
            .SingleAsync(recipe => recipe.Id == recipeId, cancellationToken);
        var emptyDraft = await db.Recipes.AsNoTracking()
            .SingleAsync(recipe => recipe.Id == emptyDraftId, cancellationToken);
        stored.Steps.Select(step => step.Position).Should().Equal(0, 1, 2);
        stored.Steps.Select(step => step.Content).Should().Equal(steps);
        stored.Tags.Select(tag => tag.Name).Should().BeEquivalentTo(tags);
        emptyDraft.Steps.Should().BeEmpty();
        emptyDraft.Tags.Should().BeEmpty();
        db.Database.HasPendingModelChanges().Should().BeFalse();

        await migrator.MigrateAsync(previousMigration, cancellationToken);
        await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT "Steps", "Tags" FROM "Happymeal"."Recipes" WHERE "Id" = @id
            """, connection);
        command.Parameters.AddWithValue("id", recipeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var hasRow = await reader.ReadAsync(cancellationToken);

        hasRow.Should().BeTrue();
        reader.GetFieldValue<string[]>(0).Should().Equal(steps);
        reader.GetFieldValue<string[]>(1).Should().BeEquivalentTo(tags);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_WhenLegacyNamesExist_ShouldPreserveDataOrRejectCollision(bool collision)
    {
        var token=TestContext.Current.CancellationToken;
        await using var postgres=new PostgreSqlBuilder("postgres:18-alpine").Build();
        await postgres.StartAsync(token);
        var options=new DbContextOptionsBuilder<HappymealDbContext>().UseNpgsql(postgres.GetConnectionString()).Options;
        await using var db=new HappymealDbContext(options);
        var migrator=db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261031120108_InitialCreate",token);
        string[] names=collision ? [" SÓPA ","sopa"] : [" TÓRTILLA ","pingüino","Sopa de ajo"];
        foreach(var name in names)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Happymeal\".\"Recipes\" (\"Id\",\"Name\",\"Description\",\"CreatedOn\",\"CreatedBy\") VALUES ({Guid.NewGuid()},{name},{"Original"},{DateTimeOffset.UnixEpoch},{"legacy"})",token);
        }
        if(collision)
        {
            var action=async()=>await migrator.MigrateAsync(cancellationToken:token);
            await action.Should().ThrowAsync<PostgresException>();
            var applied=await db.Database.GetAppliedMigrationsAsync(token);
            applied.Should().ContainSingle();
            return;
        }
        await migrator.MigrateAsync(cancellationToken:token);
        var recipes=await db.Recipes.AsNoTracking().ToListAsync(token);
        recipes.Should().HaveCount(names.Length);
        foreach(var recipe in recipes)
        {
            names.Select(name => name.Trim()).Should().Contain(recipe.Name);
            recipe.Version.Should().Be(1);
            recipe.IsPublished.Should().BeFalse();
            recipe.IsHidden.Should().BeFalse();
            recipe.CreatedBy.Should().Be("legacy");
            recipe.CreatedOn.Should().Be(DateTimeOffset.UnixEpoch);
            recipe.Description.Should().Be("Original");
        }
    }
}
