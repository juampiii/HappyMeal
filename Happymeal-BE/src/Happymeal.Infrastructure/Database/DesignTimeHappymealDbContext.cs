using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Happymeal.Infrastructure.Database;

/// <summary>Used only by the dotnet-ef CLI. The connection string is never opened when adding a migration.</summary>
internal sealed class DesignTimeHappymealDbContext : IDesignTimeDbContextFactory<HappymealDbContext>
{
    public HappymealDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HappymealDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=happymeal")
            .Options;

        return new HappymealDbContext(options);
    }
}
