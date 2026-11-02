using Happymeal.Domain;
using Happymeal.Domain.Base;
using Happymeal.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Happymeal.Infrastructure.Database;

public class HappymealDbContext : DbContext
{
    public const string Schema = "Happymeal";

    public DbSet<Recipe> Recipes { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    public HappymealDbContext(DbContextOptions<HappymealDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HappymealDbContext).Assembly);
        ConfigureAuditFields(modelBuilder);
    }

    private static void ConfigureAuditFields(ModelBuilder modelBuilder)
    {
        var auditableTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(IAuditable).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType);

        foreach (var auditableType in auditableTypes)
        {
            var builder = modelBuilder.Entity(auditableType);
            builder.Property(nameof(IAuditable.CreatedBy)).HasMaxLength(IAuditable.UserMaxLength).IsRequired();
            builder.Property(nameof(IAuditable.ModifiedBy)).HasMaxLength(IAuditable.UserMaxLength);
        }
    }
}
