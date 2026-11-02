using Happymeal.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Happymeal.IntegrationTests.Infrastructure;

/// <summary>Base class for integration tests. Resets the database before each test.</summary>
[Collection(HappymealCollection.Name)]
public abstract class TestBase(HappymealApiFactory factory) : IAsyncLifetime
{
    protected HappymealApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    public virtual async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public virtual ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    protected async Task SeedAsync(Func<HappymealDbContext, Task> seed)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HappymealDbContext>();
        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    protected async Task<T> QueryAsync<T>(Func<HappymealDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HappymealDbContext>();
        return await query(dbContext);
    }
}
