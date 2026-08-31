using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StudyTime.Api.Errors;
using System.Text.Json;

namespace Api.Tests.Errors;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task ShouldMapArgumentExceptionToBadRequestProblemDetails()
    {
        var result = await ExecuteAsync(new ArgumentException("The name is required."));

        Assert.Equal(400, result.Status);
        Assert.Equal("Invalid request.", result.Title);
        Assert.Equal("The name is required.", result.Detail);
        Assert.Equal("https://httpstatuses.com/400", result.Type);
    }

    [Fact]
    public async Task ShouldMapArgumentOutOfRangeExceptionToBadRequestProblemDetails()
    {
        var result = await ExecuteAsync(new ArgumentOutOfRangeException("minutes", "Study minutes must be greater than zero."));

        Assert.Equal(400, result.Status);
        Assert.Equal("Invalid request.", result.Title);
        Assert.Contains("Study minutes must be greater than zero.", result.Detail);
        Assert.Equal("https://httpstatuses.com/400", result.Type);
    }

    [Fact]
    public async Task ShouldMapKeyNotFoundExceptionToNotFoundProblemDetails()
    {
        var result = await ExecuteAsync(new KeyNotFoundException("Study area was not found."));

        Assert.Equal(404, result.Status);
        Assert.Equal("Resource not found.", result.Title);
        Assert.Equal("Study area was not found.", result.Detail);
        Assert.Equal("https://httpstatuses.com/404", result.Type);
    }

    [Fact]
    public async Task ShouldMapInvalidOperationExceptionToConflictProblemDetails()
    {
        var result = await ExecuteAsync(new InvalidOperationException("An inactive study plan cannot be used in a study area week."));

        Assert.Equal(409, result.Status);
        Assert.Equal("Conflict.", result.Title);
        Assert.Equal("An inactive study plan cannot be used in a study area week.", result.Detail);
        Assert.Equal("https://httpstatuses.com/409", result.Type);
    }

    [Fact]
    public async Task ShouldMapUnexpectedExceptionToInternalServerError()
    {
        // Usa uma exceção específica que não é mapeada, garantindo o fallback para 500
        var result = await ExecuteAsync(new NotSupportedException("Database password must never be exposed."));

        Assert.Equal(500, result.Status);
        Assert.Equal("Internal server error.", result.Title);
        Assert.Equal("An unexpected error occurred.", result.Detail);
        Assert.DoesNotContain("Database password", result.Detail);
        Assert.Equal("https://httpstatuses.com/500", result.Type);
    }

    [Fact]
    public async Task ShouldWriteProblemDetailsAsJson()
    {
        await using var serviceProvider = CreateServiceProvider();
        var problemDetailsService = serviceProvider.GetRequiredService<IProblemDetailsService>();
        var handler = new ApiExceptionHandler(problemDetailsService, NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(httpContext, new ArgumentException("Invalid value."), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.StartsWith("application/problem+json", httpContext.Response.ContentType);

        responseBody.Position = 0;
        var json = await new StreamReader(responseBody).ReadToEndAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Invalid request.", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Invalid value.", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("https://httpstatuses.com/400", document.RootElement.GetProperty("type").GetString());
    }

    private static async Task<ProblemDetailsResult> ExecuteAsync(Exception exception)
    {
        await using var serviceProvider = CreateServiceProvider();
        var problemDetailsService = serviceProvider.GetRequiredService<IProblemDetailsService>();
        var handler = new ApiExceptionHandler(problemDetailsService, NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);

        responseBody.Position = 0;
        var json = await new StreamReader(responseBody).ReadToEndAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new ProblemDetailsResult(
            root.GetProperty("status").GetInt32(),
            root.GetProperty("title").GetString()!,
            root.GetProperty("detail").GetString()!,
            root.GetProperty("type").GetString()!);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        return services.BuildServiceProvider();
    }

    private sealed record ProblemDetailsResult(int Status, string Title, string Detail, string Type);
}