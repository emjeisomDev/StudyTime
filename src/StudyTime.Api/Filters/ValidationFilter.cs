using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StudyTime.Api.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                        .ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
