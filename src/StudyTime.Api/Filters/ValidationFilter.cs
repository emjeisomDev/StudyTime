using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace StudyTime.Api.Filters;

public sealed class ValidationFilter(ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    private readonly ProblemDetailsFactory _problemDetailsFactory =
        problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));

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

            var problem = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext,
                context.ModelState,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: "One or more validation errors occurred.",
                instance: context.HttpContext.Request.Path);

            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
