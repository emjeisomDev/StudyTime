using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R08_WeekLockTests : IAsyncLifetime
{
    private static readonly DateOnly CurrentWeekStart = new(2026, 10, 5);
    private static readonly DateOnly NextWeekStart = new(2026, 10, 12);

    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public R08_WeekLockTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task WeekLock_GlobalGoalNotReached_BlocksCreateUpdateAndDelete()
    {
        var initial = await BootstrapAsync();
        var anotherAreaId = await CreateStudyAreaAsync("História", 1500);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/study-area-weeks",
            new
            {
                studyAreaId = anotherAreaId,
                studyPlanId = initial.StudyPlanId,
                weekStartDate = NextWeekStart
            });

        Assert.Equal(HttpStatusCode.Conflict, createResponse.StatusCode);
        await AssertProblemDetailsAsync(createResponse, 409);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/study-area-weeks/{initial.StudyAreaWeekId}",
            new
            {
                studyAreaId = anotherAreaId,
                studyPlanId = initial.StudyPlanId
            });

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        await AssertProblemDetailsAsync(updateResponse, 409);
        var deleteResponse = await _client.DeleteAsync($"/api/v1/study-area-weeks/{initial.StudyAreaWeekId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        await AssertProblemDetailsAsync(deleteResponse, 409);
    }

    [Fact]
    public async Task WeekLock_GlobalGoalReached_AllowsCreateUpdateAndDelete()
    {
        var initial = await BootstrapAsync();
        var anotherAreaId = await CreateStudyAreaAsync("História", 1500);

        var recordResponse = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{initial.StudyAreaWeekId}/study-records",
            new { minutes = 1500 }
        );

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/study-area-weeks",
            new
            {
                studyAreaId = anotherAreaId,
                studyPlanId = initial.StudyPlanId,
                weekStartDate = NextWeekStart
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/study-area-weeks/{initial.StudyAreaWeekId}",
            new
            {
                studyAreaId = anotherAreaId,
                studyPlanId = initial.StudyPlanId
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/study-area-weeks/{initial.StudyAreaWeekId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private async Task<(Guid StudyAreaWeekId, Guid StudyPlanId)> BootstrapAsync()
    {
        var studyAreaId = await CreateStudyAreaAsync("Matemática", 1500);
        var studyPlanId = await CreateStudyPlanAsync("Plano padrão", 1.00m);
        var response = await _client.PostAsJsonAsync("/bootstrap",
            new
            {
                items = new[]
                {
                    new { studyAreaId, studyPlanId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var configuration = Assert.Single(document.RootElement.EnumerateArray());

        return (configuration.GetProperty("id").GetGuid(), studyPlanId);
    }

    private async Task<Guid> CreateStudyAreaAsync(string name, int standardWeeklyMinutes)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name,
                stdWeekStudyTime = standardWeeklyMinutes
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateStudyPlanAsync(string name, decimal coefficient)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name,
                coefficient
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, int expectedStatus)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("title", out _));
        Assert.True(document.RootElement.TryGetProperty("detail", out _));
    }
}
