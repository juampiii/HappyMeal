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
            names.Should().Contain(recipe.Name);
            recipe.NameKey.Should().Be(RecipeName.Key(recipe.Name));
            recipe.Version.Should().Be(1);
            recipe.IsPublished.Should().BeFalse();
            recipe.IsHidden.Should().BeFalse();
            recipe.CreatedBy.Should().Be("legacy");
            recipe.CreatedOn.Should().Be(DateTimeOffset.UnixEpoch);
            recipe.Description.Should().Be("Original");
        }
    }
}
