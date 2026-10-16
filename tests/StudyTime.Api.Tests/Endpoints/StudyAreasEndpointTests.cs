using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Endpoints;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class StudyAreasEndpointTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public StudyAreasEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateStudyArea_ValidRequest_Returns201AndCanBeRetrieved()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name = "Matemática",
                stdWeekStudyTime = 1500
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var id = created.RootElement.GetProperty("id").GetGuid();

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Matemática", created.RootElement.GetProperty("name").GetString());
        Assert.Equal(1500, created.RootElement.GetProperty("stdWeekStudyTime").GetInt32());

        var getResponse = await _client.GetAsync($"/api/v1/study-areas/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var retrieved = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(id, retrieved.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Matemática", retrieved.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateStudyArea_ZeroStandardWeeklyMinutes_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name = "Matemática",
                stdWeekStudyTime = 0
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            "json",
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(
            problem.RootElement.TryGetProperty("errors", out _), 
            "The validation response must contain the “errors” field.");
    }

    [Fact]
    public async Task GetStudyArea_UnknownIdentifier_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/v1/study-areas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("title", out _));
        Assert.True(problem.RootElement.TryGetProperty("detail", out _));
    }
}
