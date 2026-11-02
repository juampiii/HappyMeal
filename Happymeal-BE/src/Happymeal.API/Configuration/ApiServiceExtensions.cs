using Happymeal.API.Services;
using Happymeal.Application.Common.Interfaces;
using Scalar.AspNetCore;

namespace Happymeal.API.Configuration;

public static class ApiServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices()
        {
            services.AddControllers();
            services.AddProblemDetails();
            services.AddOpenApi();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpCurrentUser>();

            // TODO: add authentication and authorization when the first protected endpoint exists.

            return services;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseApiServices()
        {
            app.UseExceptionHandler();
            app.UseStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.MapHealthChecks("/health");
            app.MapControllers();

            return app;
        }
    }
}
