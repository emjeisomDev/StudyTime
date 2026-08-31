using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StudyTime.Api.Errors;

public sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(exception);

        if (problemDetails.Status >= 500)
            LogUnhandledException(logger, exception);
        else
            LogHandledException(logger, exception);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        return true;
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled exception while processing HTTP request.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Request rejected because of a handled application or domain error.")]
    private static partial void LogHandledException(ILogger logger, Exception exception);

    private static ProblemDetails CreateProblemDetails(Exception exception)
    {
        return exception switch
        {
            ArgumentOutOfRangeException => Create(400, "Invalid request.", exception.Message),
            ArgumentException => Create(400, "Invalid request.", exception.Message),
            KeyNotFoundException => Create(404, "Resource not found.", exception.Message),
            InvalidOperationException => Create(409, "Conflict.", exception.Message),
            _ => Create(500, "Internal server error.", "An unexpected error occurred.")
        };
    }

    private static ProblemDetails Create(int status, string title, string detail)
        => new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}"
        };
}