using System.Text.Json;
using Microsoft.AspNetCore.Http;
using StudyTime.Application.Exceptions;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Api.Middleware;

public sealed class ProblemDetailsExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger;

    public ProblemDetailsExceptionMiddleware(
        RequestDelegate next,
        ILogger<ProblemDetailsExceptionMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            DomainRuleViolationException => StatusCodes.Status409Conflict,
            OperationCanceledException => 499,
            _ => StatusCodes.Status500InternalServerError
        };

        string title = exception switch
        {
            ValidationException => "Validation error",
            NotFoundException => "Resource not found",
            ConflictException => "Conflict",
            DomainRuleViolationException => "Business rule violation",
            OperationCanceledException => "Request canceled",
            _ => "Internal server error"
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled error during request processing.");
        }
        else if (statusCode != 499)
        {
            _logger.LogWarning(exception, "Domain or application error during request processing.");
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response had already begun before the exception handling.");

            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        ProblemDetailsResponse response = new(
            Type: $"https://httpstatuses.com/{statusCode}",
            Title: title,
            Status: statusCode,
            Detail: exception.Message,
            Instance: context.Request.Path);

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private sealed record ProblemDetailsResponse(
        string Type,
        string Title,
        int Status,
        string Detail,
        string Instance);
}