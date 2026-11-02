using System.Data.Common;
using Happymeal.Infrastructure.Configuration;
using Happymeal.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Happymeal.IntegrationTests.Infrastructure;

/// <summary>Boots the API against a real PostgreSQL container. One instance is shared by the whole test collection.</summary>
public sealed class HappymealApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("happymeal")
        .Build();

    private Respawner _respawner = null!;
    private DbConnection _respawnConnection = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<HappymealDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        _respawnConnection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await _respawnConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [HappymealDbContext.Schema],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_respawnConnection);

    public override async ValueTask DisposeAsync()
    {
        await _respawnConnection.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            $"ConnectionStrings:{ServiceExtensions.ConnectionStringName}",
            _postgresContainer.GetConnectionString());
    }
}
