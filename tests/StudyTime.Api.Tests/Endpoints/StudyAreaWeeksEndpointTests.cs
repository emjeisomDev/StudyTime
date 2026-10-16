using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Endpoints;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class StudyAreaWeeksEndpointTests : IAsyncLifetime
{
    private static readonly DateOnly CurrentWeekStart = new(2026, 10, 5);
    private static readonly DateOnly NextWeekStart = new(2026, 10, 12);
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public StudyAreaWeeksEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetStudyAreaWeek_BootstrappedConfiguration_Returns200()
    {
        var ids = await CreateBootstrapWeekAsync();

        var response = await _client.GetAsync($"/api/v1/study-area-weeks/{ids.StudyAreaWeekId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(ids.StudyAreaWeekId, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(CurrentWeekStart, DateOnly.Parse(body.RootElement.GetProperty("weekStartDate").GetString()!));
        Assert.Equal(1500m, body.RootElement.GetProperty("weekIndividualGoal").GetDecimal());
        Assert.Equal(ids.WeeklyAssessmentId, body.RootElement.GetProperty("weeklyAssessmentId").GetGuid());
    }

    [Fact]
    public async Task CreateBatch_WeeklyGoalNotReached_Returns409ProblemDetails()
    {
        var ids = await CreateBootstrapWeekAsync();
        var secondAreaId = await CreateStudyAreaAsync("História", 1500);

        var response = await _client.PostAsJsonAsync("/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = NextWeekStart,
                items = new[]
                {
                    new
                    {
                        studyAreaId = secondAreaId,
                        studyPlanId = ids.StudyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Conflict", problem.RootElement.GetProperty("title").GetString());
        Assert.True(problem.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task CreateBatch_CurrentGlobalGoalReached_Returns201()
    {
        var ids = await CreateBootstrapWeekAsync();
        var secondAreaId = await CreateStudyAreaAsync("História", 1500);

        var recordResponse = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{ids.StudyAreaWeekId}/study-records",
            new { minutes = 1500 });

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = NextWeekStart,
                items = new[]
                {
                    new
                    {
                        studyAreaId = secondAreaId,
                        studyPlanId = ids.StudyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var configurations = body.RootElement;

        Assert.Equal(JsonValueKind.Array, configurations.ValueKind);
        Assert.Single(configurations.EnumerateArray());

        var createdWeek = configurations[0];

        Assert.Equal(NextWeekStart, DateOnly.Parse(createdWeek.GetProperty("weekStartDate").GetString()!));
        Assert.Equal(secondAreaId, createdWeek.GetProperty("studyAreaId").GetGuid());
        Assert.Equal(1500m, createdWeek.GetProperty("weekIndividualGoal").GetDecimal());
    }

    [Fact]
    public async Task CreateBatch_NonMondayWeekStart_Returns400ProblemDetails()
    {
        var ids = await CreateBootstrapWeekAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = new DateOnly(2026, 10, 13),
                items = new[]
                {
                    new
                    {
                        studyAreaId = await CreateStudyAreaAsync("Geografia", 1500),
                        studyPlanId = ids.StudyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    private async Task<(Guid StudyAreaWeekId, Guid StudyPlanId, Guid WeeklyAssessmentId)> CreateBootstrapWeekAsync()
    {
        var studyAreaId = await CreateStudyAreaAsync("Matemática", 1500);
        var studyPlanId = await CreateStudyPlanAsync("Plano padrão", 1.00m);

        var response = await _client.PostAsJsonAsync("/bootstrap",
            new
            {
                items = new[]
                {
                    new
                    {
                        studyAreaId,
                        studyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var item = Assert.Single(body.RootElement.EnumerateArray());

        return (
            item.GetProperty("id").GetGuid(),
            studyPlanId,
            item.GetProperty("weeklyAssessmentId").GetGuid());
    }

    private async Task<Guid> CreateStudyAreaAsync(string name, int minutes)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name,
                stdWeekStudyTime = minutes
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
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

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
    }
}
