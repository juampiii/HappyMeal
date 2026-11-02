using Happymeal.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Happymeal.API.Extensions;

public static class ControllerExtensions
{
    extension(ControllerBase controller)
    {
        /// <summary>Success becomes 204 No Content. Errors become ProblemDetails.</summary>
        public IActionResult ToActionResult(Result result)
        {
            return result.IsSuccess
                ? controller.NoContent()
                : controller.ToProblem(result.Error!);
        }

        /// <summary>Success becomes 200 OK, or the <paramref name="onSuccess"/> result. Errors become ProblemDetails.</summary>
        public IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult>? onSuccess = null)
        {
            if (result.IsFailure)
            {
                return controller.ToProblem(result.Error!);
            }

            return onSuccess is not null
                ? onSuccess(result.Value)
                : controller.Ok(result.Value);
        }

        private IActionResult ToProblem(Error error)
        {
            return error.Type switch
            {
                ErrorType.Validation => controller.ValidationProblem(controller.ToModelState(error.ValidationErrors)),
                ErrorType.BadRequest => controller.Problem(error.Message, statusCode: StatusCodes.Status400BadRequest),
                ErrorType.NotFound => controller.Problem(error.Message, statusCode: StatusCodes.Status404NotFound),
                ErrorType.Conflict => controller.Problem(error.Message, statusCode: StatusCodes.Status409Conflict),
                _ => controller.Problem(error.Message, statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        /// <summary>Converts C# property paths (<c>Steps[0].Text</c>) to the JSON names the client sent (<c>steps[0].text</c>).</summary>
        private ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> errors)
        {
            var namingPolicy = controller.HttpContext.RequestServices
                .GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions.PropertyNamingPolicy;

            var modelState = new ModelStateDictionary();
            foreach (var (propertyPath, messages) in errors)
            {
                var jsonPath = namingPolicy is null
                    ? propertyPath
                    : string.Join('.', propertyPath.Split('.').Select(namingPolicy.ConvertName));

                foreach (var message in messages)
                {
                    modelState.AddModelError(jsonPath, message);
                }
            }

            return modelState;
        }
    }
}
