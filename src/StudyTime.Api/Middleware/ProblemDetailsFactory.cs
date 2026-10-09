using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Api.Middleware;

public sealed class ProblemDetailsFactory
{
    public ProblemDetails Create(Exception exception, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(httpContext);

        var (statusCode, title, detail) = exception switch
        {
            RecordNotInCurrentWeekException =>
                (StatusCodes.Status409Conflict,
                 "Conflict",
                 exception.Message),

            WeekConfigurationLockedException =>
                (StatusCodes.Status409Conflict,
                 "Conflict",
                 exception.Message),

            DuplicateStudyAreaWeekException =>
                (StatusCodes.Status409Conflict,
                 "Conflict",
                 exception.Message),

            WeeklyGoalNotMetException =>
                (StatusCodes.Status422UnprocessableEntity,
                 "Unprocessable Entity",
                 exception.Message),

            ValidationException =>
                (StatusCodes.Status400BadRequest,
                 "Validation Error",
                 "One or more validation errors occurred."),

            DomainException =>
                (StatusCodes.Status422UnprocessableEntity,
                 "Domain Rule Violation",
                 exception.Message),

            BadHttpRequestException =>
                (StatusCodes.Status400BadRequest,
                 "Bad Request",
                 exception.Message),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "Internal Server Error",
                 "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct()
                        .ToArray());
        }

        return problem;
    }
}
