using Npgsql;
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

        var postgresException = FindPostgresException(exception);
        var (statusCode, title, detail) = postgresException is not null ? postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation =>
                    (StatusCodes.Status409Conflict, "Conflict", "The requested resource conflicts with existing data."),
                PostgresErrorCodes.ForeignKeyViolation =>
                    (StatusCodes.Status409Conflict, "Conflict", "The operation conflicts with a related resource."),
                PostgresErrorCodes.CheckViolation or PostgresErrorCodes.NotNullViolation or PostgresErrorCodes.NumericValueOutOfRange =>
                    (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity", "The supplied data violates a persistence constraint."),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected database error occurred.")
            }
            : exception switch
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

                KeyNotFoundException =>
                        (StatusCodes.Status404NotFound,
                         "Not Found",
                         exception.Message),

                DomainException =>
                        (StatusCodes.Status422UnprocessableEntity,
                         "Domain Rule Violation",
                         exception.Message),

                ArgumentException =>
                        (StatusCodes.Status422UnprocessableEntity,
                         "Invalid Request",
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

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}
