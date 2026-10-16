using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R09A_BatchEndpointTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public R09A_BatchEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateBatch_ValidItems_CreatesAllConfigurationsAtomically()
    {
        var firstAreaId = await CreateStudyAreaAsync("Matemática", 750);
        var secondAreaId = await CreateStudyAreaAsync("História", 750);
        var planId = await CreateStudyPlanAsync("Plano padrão", 1.00m);
        var currentWeekStart = await BootstrapCurrentWeekAndReachGlobalGoalAsync(planId);
        var batchWeekStart = currentWeekStart.AddDays(7);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = batchWeekStart,
                items = new[]
                {
                    new { studyAreaId = firstAreaId, studyPlanId = planId },
                    new { studyAreaId = secondAreaId, studyPlanId = planId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var configurations = document.RootElement.EnumerateArray().ToArray();

        Assert.Equal(2, configurations.Length);
        Assert.Equal(
            2,
            configurations.Select(item => item.GetProperty("id").GetGuid())
                .Distinct()
                .Count());

        Assert.All(
            configurations,
            item => Assert.Equal(
                batchWeekStart,
                DateOnly.Parse(
                    item.GetProperty("weekStartDate").GetString()!)));

        Assert.Single(
            configurations.Select(
                    item => item.GetProperty("weeklyAssessmentId").GetGuid())
                .Distinct());

        Assert.All(
            configurations,
            item => Assert.Equal(
                750m,
                item.GetProperty("weekIndividualGoal").GetDecimal()));

        Assert.Equal(
            new HashSet<Guid> { firstAreaId, secondAreaId },
            configurations.Select(
                    item => item.GetProperty("studyAreaId").GetGuid())
                .ToHashSet());
    }

    [Fact]
    public async Task CreateBatch_SecondItemHasUnknownPlan_DoesNotPersistPartialConfiguration()
    {
        var firstAreaId = await CreateStudyAreaAsync("Matemática", 1500);
        var secondAreaId = await CreateStudyAreaAsync("História", 1500);
        var planId = await CreateStudyPlanAsync("Plano padrão", 1.00m);
        var currentWeekStart = await BootstrapCurrentWeekAndReachGlobalGoalAsync(planId);
        var batchWeekStart = currentWeekStart.AddDays(7);

        var failedResponse = await _client.PostAsJsonAsync(
            "/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = batchWeekStart,
                items = new[]
                {
                    new { studyAreaId = firstAreaId, studyPlanId = planId },
                    new
                    {
                        studyAreaId = secondAreaId,
                        studyPlanId = Guid.NewGuid()
                    }
                }
            });

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            failedResponse.StatusCode);

        Assert.Equal(
            "application/problem+json",
            failedResponse.Content.Headers.ContentType?.MediaType);

        using (var problem = JsonDocument.Parse(
                   await failedResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(
                500,
                problem.RootElement.GetProperty("status").GetInt32());
        }

        // Reutilizar a mesma área na mesma semana comprova que a tentativa
        // anterior não deixou uma StudyAreaWeek persistida parcialmente.
        var retryResponse = await _client.PostAsJsonAsync(
            "/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = batchWeekStart,
                items = new[]
                {
                    new { studyAreaId = firstAreaId, studyPlanId = planId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);

        using var result = JsonDocument.Parse(
            await retryResponse.Content.ReadAsStringAsync());

        var created = Assert.Single(result.RootElement.EnumerateArray());

        Assert.Equal(
            firstAreaId,
            created.GetProperty("studyAreaId").GetGuid());

        Assert.Equal(
            1500m,
            created.GetProperty("weekIndividualGoal").GetDecimal());
    }

    private async Task<DateOnly> BootstrapCurrentWeekAndReachGlobalGoalAsync(Guid planId)
    {
        // R08 exige que a meta global da semana corrente esteja atingida antes
        // de permitir a configuração manual de uma semana seguinte.
        var bootstrapAreaId = await CreateStudyAreaAsync("Área-base do bootstrap", 1500);

        var bootstrapResponse = await _client.PostAsJsonAsync(
            "/bootstrap",
            new
            {
                items = new[]
                {
                    new { studyAreaId = bootstrapAreaId, studyPlanId = planId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        using var bootstrapDocument = JsonDocument.Parse(
            await bootstrapResponse.Content.ReadAsStringAsync());

        var bootstrapConfiguration = Assert.Single(
            bootstrapDocument.RootElement.EnumerateArray());

        var currentWeekStart = DateOnly.Parse(
            bootstrapConfiguration.GetProperty("weekStartDate").GetString()!);
        var currentStudyAreaWeekId = bootstrapConfiguration.GetProperty("id").GetGuid();

        var recordResponse = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{currentStudyAreaWeekId}/study-records",
            new { minutes = 1500 });

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);

        return currentWeekStart;
    }

    private async Task<Guid> CreateStudyAreaAsync(string name, int minutes)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/study-areas",
            new
            {
                name,
                stdWeekStudyTime = minutes
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateStudyPlanAsync(string name, decimal coefficient)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/study-plans",
            new
            {
                name,
                coefficient
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }
}
