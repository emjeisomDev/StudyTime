using System.Net;
using System.Text.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Endpoints;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class WeeksEndpointTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public WeeksEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPreviousWeekSummary_NoPreviousAssessment_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/weeks/previous-summary");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Previous week summary not found", problem.RootElement.GetProperty("title").GetString());

        Assert.True(problem.RootElement.TryGetProperty("detail", out var detail));
        Assert.False(string.IsNullOrWhiteSpace(detail.GetString()));
    }
}