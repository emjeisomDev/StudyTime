using System.Text.Json;

namespace StudyTime.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ProblemDetailsFactory problemDetailsFactory)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(problemDetailsFactory);

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Request {TraceIdentifier} was cancelled by the client.",
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "An exception occurred after the response had started. TraceId: {TraceIdentifier}",
                    context.TraceIdentifier);

                throw;
            }

            var problem = problemDetailsFactory.Create(exception, context);

            if (problem.Status is >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}. TraceId: {TraceIdentifier}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogInformation(
                    "Request rejected with status {StatusCode}. TraceId: {TraceIdentifier}",
                    problem.Status,
                    context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode =
                problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problem, JsonOptions),
                context.RequestAborted);
        }
    }
}
