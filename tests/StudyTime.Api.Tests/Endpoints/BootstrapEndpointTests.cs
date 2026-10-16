using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Endpoints;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class BootstrapEndpointTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public BootstrapEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Bootstrap_ValidInitialConfiguration_Returns201()
    {
        var studyAreaId = await CreateStudyAreaAsync();
        var studyPlanId = await CreateStudyPlanAsync();

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

        var configuration = Assert.Single(body.RootElement.EnumerateArray());

        Assert.NotEqual(Guid.Empty, configuration.GetProperty("id").GetGuid());
        Assert.Equal(studyAreaId, configuration.GetProperty("studyAreaId").GetGuid());
        Assert.Equal(studyPlanId, configuration.GetProperty("studyPlanId").GetGuid());
        Assert.Equal(1500m, configuration.GetProperty("weekIndividualGoal").GetDecimal());
        Assert.Equal(DateOnly.Parse("2026-10-05"), DateOnly.Parse(configuration.GetProperty("weekStartDate").GetString()!));

    }

    [Fact]
    public async Task Bootstrap_SystemAlreadyInitialized_Returns422ProblemDetails()
    {
        var studyAreaId = await CreateStudyAreaAsync();
        var studyPlanId = await CreateStudyPlanAsync();

        var firstResponse = await _client.PostAsJsonAsync("/bootstrap",
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

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/bootstrap",
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

        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondResponse.StatusCode);

        using var problem = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());

        Assert.Equal(422, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("title", out _));
        Assert.True(problem.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task Bootstrap_EmptyItems_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/bootstrap",
            new
            {
                items = Array.Empty<object>()
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    private async Task<Guid> CreateStudyAreaAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name = "Matemática",
                stdWeekStudyTime = 1500
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateStudyPlanAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name = "Plano padrão",
                coefficient = 1.00m
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("id").GetGuid();
    }
}