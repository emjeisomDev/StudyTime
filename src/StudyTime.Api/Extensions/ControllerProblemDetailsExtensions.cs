using Microsoft.AspNetCore.Mvc;

namespace StudyTime.Api.Extensions;
public static class ControllerProblemDetailsExtensions
{
    public static ProblemDetails CreateProblemDetails(
        this ControllerBase controller,
        int status,
        string title,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        ProblemDetails problem;
        if (status == StatusCodes.Status400BadRequest)
        {
            problem = new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["request"] = new[] { detail } })
            {
                Type = "about:blank",
                Status = status,
                Title = title,
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            };
        }
        else
        {
            problem = new ProblemDetails
            {
                Type = "about:blank",
                Status = status,
                Title = title,
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            };
        }

        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;
        return problem;
    }
}