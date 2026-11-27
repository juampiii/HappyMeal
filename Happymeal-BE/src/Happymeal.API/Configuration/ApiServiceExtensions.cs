using Happymeal.API.Services;
using Happymeal.API.Mappers;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

namespace Happymeal.API.Configuration;

public static class ApiServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices()
        {
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(
                    ProblemDetailsMapper.MapToInvalidRequestProblemDetails(context));
            });
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    var code = context.ProblemDetails.Status switch
                    {
                        StatusCodes.Status400BadRequest => ErrorCodes.InvalidRequest,
                        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
                        StatusCodes.Status405MethodNotAllowed => ErrorCodes.MethodNotAllowed,
                        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
                        StatusCodes.Status415UnsupportedMediaType => ErrorCodes.UnsupportedMediaType,
                        >= StatusCodes.Status500InternalServerError => ErrorCodes.Unexpected,
                        _ => ErrorCodes.HttpError
                    };
                    context.ProblemDetails.Extensions.TryAdd("code", code);
                };
            });
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
