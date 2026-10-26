using Npgsql;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace StudyTime.Api.Middleware;

public sealed class ProblemDetailsFactory
{
    public ProblemDetails Create(Exception exception, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

            return CreateValidationProblemDetails(
                errors,
                httpContext,
                "One or more validation errors occurred.");
        }

        if (exception is BadHttpRequestException)
        {
            return CreateValidationProblemDetails(
                new Dictionary<string, string[]> { ["request"] = new[] { "The request format is invalid." } },
                httpContext,
                "The HTTP request could not be parsed.");
        }

        var postgresException = FindPostgresException(exception);
        var (statusCode, title, detail) = postgresException is not null ? postgresException.SqlState switch
        {
            "40001" or "40P01" =>
                (StatusCodes.Status409Conflict, "Concurrency Conflict", "A concurrent transaction changed related data. Reload the current state and retry the operation."),
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
                DbUpdateConcurrencyException =>
                        (StatusCodes.Status409Conflict,
                         "Concurrency Conflict",
                         "The resource changed during this operation. Reload the current state and retry."),

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

                NoEligibleStudyRecordException =>
                        (StatusCodes.Status404NotFound,
                         "Not Found",
                         exception.Message),

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


                _ =>
                        (StatusCodes.Status500InternalServerError,
                         "Internal Server Error",
                         "An unexpected error occurred.")
            };

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problem;
    }

    public ValidationProblemDetails CreateValidationProblemDetails(
        IDictionary<string, string[]> errors,
        HttpContext httpContext,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        var problem = new ValidationProblemDetails(errors)
        {
            Type = "about:blank",
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
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
