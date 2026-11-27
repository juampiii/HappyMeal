using Happymeal.Common.Results;
using Happymeal.API.Mappers;
using Microsoft.AspNetCore.Mvc;

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
            var problem = ProblemDetailsMapper.MapToProblemDetails(error, controller.HttpContext);
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }
    }
}
