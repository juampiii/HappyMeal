using Happymeal.API.Dtos;
using Happymeal.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Happymeal.API.Mappers;

public static class ProblemDetailsMapper
{
    private static readonly IReadOnlyDictionary<string, object> EmptyParameters = new Dictionary<string, object>();

    public static ProblemDetails MapToProblemDetails(Error error, HttpContext httpContext)
    {
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        if (error.Type == ErrorType.Validation)
        {
            var namingPolicy = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>()
                .Value.JsonSerializerOptions.PropertyNamingPolicy;
            string JsonPath(string path) => namingPolicy is null
                ? path
                : string.Join('.', path.Split('.').Select(namingPolicy.ConvertName));

            var modelState = new ModelStateDictionary();
            foreach (var (propertyPath, messages) in error.ValidationErrors)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(JsonPath(propertyPath), message);
                }
            }

            var validation = factory.CreateValidationProblemDetails(httpContext, modelState);
            validation.Extensions["code"] = error.Code;
            validation.Extensions["validationErrors"] = error.ValidationFailures
                .GroupBy(failure => JsonPath(failure.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(failure => new ValidationErrorDto(
                    failure.Code, failure.Parameters ?? EmptyParameters)).ToArray());
            return validation;
        }

        var status = error.Type switch
        {
            ErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        var problem = factory.CreateProblemDetails(httpContext, statusCode: status, detail: error.Message);
        problem.Extensions["code"] = error.Code;
        return problem;
    }

    public static ValidationProblemDetails MapToInvalidRequestProblemDetails(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
        problem.Extensions["code"] = ErrorCodes.InvalidRequest;
        problem.Extensions["validationErrors"] = context.ModelState
            .Where(field => field.Value is { Errors.Count: > 0 })
            .ToDictionary(field => field.Key, field => field.Value!.Errors.Select(_ =>
                new ValidationErrorDto(ErrorCodes.InvalidRequestValue, EmptyParameters)).ToArray());
        return problem;
    }
}
