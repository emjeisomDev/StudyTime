using System.Net;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R17_DeleteOutsideWeekTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _databaseResetFixture;
    private readonly ApiWebApplicationFactory _factory;

    public R17_DeleteOutsideWeekTests(DatabaseResetFixture databaseResetFixture)
    {
        _databaseResetFixture = databaseResetFixture;
        _factory = databaseResetFixture.Factory;
    }

    public async Task InitializeAsync()
    {
        await _databaseResetFixture.ResetAsync();
        SetClock("2026-10-05T15:00:00Z");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteLast_WhenRecordBelongsToPreviousWeek_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        var studyAreaId = await CreateStudyAreaAsync(client, "R17 - Área de estudo", 1500);
        var studyPlanId = await CreateStudyPlanAsync(client, "R17 - Plano de estudo", 1.0m);
        var studyAreaWeekId = await BootstrapAsync(client, studyAreaId, studyPlanId);

        await CreateRecordAsync(client, studyAreaWeekId, 30);

        // A configuração e o registro passam a pertencer à semana anterior.
        SetClock("2026-10-12T15:00:00Z");

        using var deleteResponse = await client.DeleteAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records/last");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal("application/problem+json", deleteResponse.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());
        Assert.Equal(StatusCodes.Status409Conflict, problem.RootElement.GetProperty("status").GetInt32());

        // A rejeição não deve remover o registro.
        using var recordsResponse = await client.GetAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records");
        Assert.Equal(HttpStatusCode.OK, recordsResponse.StatusCode);

        using var recordsDocument = JsonDocument.Parse(await recordsResponse.Content.ReadAsStringAsync());
        Assert.Single(recordsDocument.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task DeleteLast_WhenConfigurationBelongsToFutureWeek_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        var studyAreaId = await CreateStudyAreaAsync(client, "R17 - Área futura", 1500);
        var studyPlanId = await CreateStudyPlanAsync(client, "R17 - Plano futuro", 1.0m);

        // R08: a configuração manual de outra semana só é permitida depois
        // que a meta global da semana corrente tiver sido atingida.
        var currentStudyAreaWeekId = await BootstrapAsync(
            client,
            studyAreaId,
            studyPlanId);
        await CreateRecordAsync(client, currentStudyAreaWeekId, 1500);

        var futureWeekStart = new DateOnly(2026, 10, 12);

        using var batchResponse = await client.PostAsJsonAsync("/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = futureWeekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                items = new[]
                {
                    new
                    {
                        studyAreaId,
                        studyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, batchResponse.StatusCode);

        using var batchDocument = JsonDocument.Parse(await batchResponse.Content.ReadAsStringAsync());

        var futureStudyAreaWeekId = batchDocument.RootElement[0].GetProperty("id").GetGuid();

        using var deleteResponse = await client.DeleteAsync($"/api/v1/study-area-weeks/{futureStudyAreaWeekId}/study-records/last");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal("application/problem+json", deleteResponse.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            StatusCodes.Status409Conflict,
            problem.RootElement.GetProperty("status").GetInt32());
    }

    private void SetClock(string utcInstant)
    {
        _factory.Clock.SetUtcNow(
            DateTime.Parse(
                utcInstant,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind));
    }

    private static async Task<Guid> BootstrapAsync(
        HttpClient client,
        Guid studyAreaId,
        Guid studyPlanId)
    {
        using var response = await client.PostAsJsonAsync("/bootstrap",
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
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement[0].GetProperty("id").GetGuid();
    }

    private static async Task CreateRecordAsync(HttpClient client, Guid studyAreaWeekId, int minutes)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records", new { minutes });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateStudyAreaAsync(HttpClient client, string name, int standardWeeklyMinutes)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name,
                stdWeekStudyTime = standardWeeklyMinutes
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateStudyPlanAsync(HttpClient client, string name, decimal coefficient)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/study-plans",
            new
            {
                name,
                coefficient
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }
}