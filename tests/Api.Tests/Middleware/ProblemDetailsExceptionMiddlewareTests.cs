using Xunit;
using System.Net;
using System.Net.Http.Json;

namespace Api.Tests.Middleware;

public sealed class ProblemDetailsExceptionMiddlewareTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public ProblemDetailsExceptionMiddlewareTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task ValidationException_Returns400ProblemDetails()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/test-exceptions/validation");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error");
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task NotFoundException_Returns404ProblemDetails()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/test-exceptions/not-found");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found");
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task ConflictException_Returns409ProblemDetails()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/test-exceptions/conflict");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict");
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task DomainRuleViolationException_Returns409ProblemDetails()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/test-exceptions/domain-rule");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Conflict,
            "Business rule violation");
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public async Task UnhandledException_Returns500ProblemDetails()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/test-exceptions/unhandled");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "Internal server error");
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedTitle)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        ProblemDetailsDto? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();

        Assert.NotNull(problemDetails);
        Assert.Equal((int)expectedStatusCode, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Type));
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Detail));
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Instance));
    }

    private sealed record ProblemDetailsDto(
        string Type,
        string Title,
        int Status,
        string Detail,
        string Instance);
}