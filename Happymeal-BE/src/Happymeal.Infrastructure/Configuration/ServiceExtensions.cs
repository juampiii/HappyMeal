using Happymeal.Application.Common.Interfaces;
using Happymeal.Infrastructure.Database;
using Happymeal.Infrastructure.Database.Interceptors;
using Happymeal.Infrastructure.Outbox;
using Happymeal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Happymeal.Infrastructure.Configuration;

public static class ServiceExtensions
{
    public const string ConnectionStringName = "Happymeal";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");
            }

            services.AddDbContext<HappymealDbContext>((serviceProvider, options) => options
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableInterceptor>()));

            services
                .AddSingleton(TimeProvider.System)
                .AddScoped<AuditableInterceptor>()
                .AddScoped<IRecipeRepository, RecipeRepository>()
                .AddScoped<IUnitOfWork, UnitOfWork>()
                .AddOutbox(configuration);

            services.AddHealthChecks()
                .AddDbContextCheck<HappymealDbContext>();

            return services;
        }

        private IServiceCollection AddOutbox(IConfiguration configuration)
        {
            services.AddOptions<OutboxOptions>()
                .Bind(configuration.GetSection(OutboxOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHostedService<OutboxDispatcher>();

            return services;
        }
    }
}
