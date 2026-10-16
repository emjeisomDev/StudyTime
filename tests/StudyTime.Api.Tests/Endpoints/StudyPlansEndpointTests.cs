using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Endpoints;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class StudyPlansEndpointTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public StudyPlansEndpointTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateStudyPlan_ValidRequest_Returns201AndCanBeRetrieved()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name = "Plano padrão",
                coefficient = 1.00m
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var id = created.RootElement.GetProperty("id").GetGuid();

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Plano padrão", created.RootElement.GetProperty("name").GetString());
        Assert.Equal(1.00m, created.RootElement.GetProperty("coefficient").GetDecimal());
        Assert.Equal("Active", created.RootElement.GetProperty("status").GetString());

        var getResponse = await _client.GetAsync($"/api/v1/study-plans/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var retrieved = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(id, retrieved.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Plano padrão", retrieved.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdateStudyPlanStatus_InactiveRequest_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name = "Plano para desativar",
                coefficient = 1.00m
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());

        var id = created.RootElement.GetProperty("id").GetGuid();
        var updateResponse = await _client.PatchAsJsonAsync($"/api/v1/study-plans/{id}/status", new { status = "inactive" });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var updated = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());

        Assert.Equal(id, updated.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Inactive", updated.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateStudyPlan_ZeroCoefficient_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name = "Plano inválido",
                coefficient = 0m
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task UpdateStudyPlanStatus_InvalidStatus_Returns422ProblemDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name = "Plano com status inválido",
                coefficient = 1.00m
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        var response = await _client.PatchAsJsonAsync($"/api/v1/study-plans/{id}/status", new { status = "paused" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(422, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Invalid study plan status", problem.RootElement.GetProperty("title").GetString());
    }
}