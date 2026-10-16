using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;


namespace StudyTime.Api.Tests.Endpoints;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class StudyRecordsEndpointTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public StudyRecordsEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAndGetStudyRecords_ValidRequest_Returns201And200()
    {
        var studyAreaWeekId = await CreateBootstrapWeekAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes = 45 });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());

        var recordId = created.RootElement.GetProperty("id").GetGuid();

        Assert.NotEqual(Guid.Empty, recordId);
        Assert.Equal(45, created.RootElement.GetProperty("minutes").GetInt32());
        Assert.Equal(studyAreaWeekId, created.RootElement.GetProperty("studyAreaWeekId").GetGuid());

        var getResponse = await _client.GetAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var records = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        var record = Assert.Single(records.RootElement.EnumerateArray());

        Assert.Equal(recordId, record.GetProperty("id").GetGuid());
        Assert.Equal(45, record.GetProperty("minutes").GetInt32());
    }

    [Fact]
    public async Task DeleteLastStudyRecord_RecordExists_Returns204AndRemovesRecord()
    {
        var studyAreaWeekId = await CreateBootstrapWeekAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes = 30 });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records/last");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var records = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Empty(records.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task CreateStudyRecord_ZeroMinutes_Returns400ProblemDetails()
    {
        var studyAreaWeekId = await CreateBootstrapWeekAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task DeleteLastStudyRecord_NoRecordsExist_Returns404ProblemDetails()
    {
        var studyAreaWeekId = await CreateBootstrapWeekAsync();

        var response = await _client.DeleteAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records/last");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("title", out _));
        Assert.True(problem.RootElement.TryGetProperty("detail", out _));
    }

    private async Task<Guid> CreateBootstrapWeekAsync()
    {
        var areaResponse = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name = "Matemática",
                stdWeekStudyTime = 1500
            });

        Assert.Equal(HttpStatusCode.Created, areaResponse.StatusCode);

        using var areaBody = JsonDocument.Parse(await areaResponse.Content.ReadAsStringAsync());

        var studyAreaId = areaBody.RootElement.GetProperty("id").GetGuid();

        var planResponse = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name = "Plano padrão",
                coefficient = 1.00m
            });

        Assert.Equal(HttpStatusCode.Created, planResponse.StatusCode);

        using var planBody = JsonDocument.Parse(await planResponse.Content.ReadAsStringAsync());

        var studyPlanId = planBody.RootElement.GetProperty("id").GetGuid();

        var bootstrapResponse = await _client.PostAsJsonAsync("/bootstrap",
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

        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        using var bootstrapBody = JsonDocument.Parse(await bootstrapResponse.Content.ReadAsStringAsync());

        var configuration = Assert.Single(bootstrapBody.RootElement.EnumerateArray());

        return configuration.GetProperty("id").GetGuid();
    }
}