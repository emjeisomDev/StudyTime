using System.Net;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R31_RecordWithinValidWeekTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private ApiWebApplicationFactory Factory => _fixture.Factory;

    public R31_RecordWithinValidWeekTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        SetClock("2026-10-05T15:00:00Z");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateRecord_WhenWeeklyGoalIsValidAndDateIsWithinWeek_ReturnsCreated()
    {
        using var client = Factory.CreateClient();

        var studyAreaId = await CreateStudyAreaAsync(
            client,
            "R31 - Área válida",
            1500);

        var studyPlanId = await CreateStudyPlanAsync(
            client,
            "R31 - Plano válido",
            1.0m);

        var studyAreaWeekId = await BootstrapAsync(
            client,
            studyAreaId,
            studyPlanId);

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes = 45 });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var recordDocument = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            studyAreaWeekId,
            recordDocument.RootElement
                .GetProperty("studyAreaWeekId")
                .GetGuid());

        Assert.Equal(
            45,
            recordDocument.RootElement.GetProperty("minutes").GetInt32());

        Assert.Equal(
            new DateOnly(2026, 10, 5),
            DateOnly.Parse(
                recordDocument.RootElement.GetProperty("date").GetString()!,
                CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Bootstrap_WhenGlobalGoalIsBelowMinimum_ReturnsUnprocessableEntity()
    {
        using var client = Factory.CreateClient();

        var studyAreaId = await CreateStudyAreaAsync(
            client,
            "R31 - Área abaixo da meta",
            1499);

        var studyPlanId = await CreateStudyPlanAsync(
            client,
            "R31 - Plano abaixo da meta",
            1.0m);

        // O bootstrap é a única forma permitida de criar a configuração inicial.
        // Com meta global de 1499 minutos, a inicialização deve ser rejeitada por R11.
        using var response = await client.PostAsJsonAsync(
            "/bootstrap",
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

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            StatusCodes.Status422UnprocessableEntity,
            problem.RootElement.GetProperty("status").GetInt32());

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.RootElement.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task CreateRecord_WhenConfigurationBelongsToAnotherWeek_ReturnsConflict()
    {
        using var client = Factory.CreateClient();

        var studyAreaId = await CreateStudyAreaAsync(
            client,
            "R31 - Área de semana anterior",
            1500);

        var studyPlanId = await CreateStudyPlanAsync(
            client,
            "R31 - Plano de semana anterior",
            1.0m);

        var studyAreaWeekId = await BootstrapAsync(
            client,
            studyAreaId,
            studyPlanId);

        // A criação de registros é restrita à semana corrente.
        SetClock("2026-10-12T15:00:00Z");

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes = 20 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            StatusCodes.Status409Conflict,
            problem.RootElement.GetProperty("status").GetInt32());
    }

    private void SetClock(string utcInstant)
    {
        Factory.Clock.SetUtcNow(
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
        using var response = await client.PostAsJsonAsync(
            "/bootstrap",
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

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement[0].GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateStudyAreaAsync(
        HttpClient client,
        string name,
        int standardWeeklyMinutes)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/study-areas",
            new
            {
                name,
                stdWeekStudyTime = standardWeeklyMinutes
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateStudyPlanAsync(
        HttpClient client,
        string name,
        decimal coefficient)
    {
        using var response = await client.PostAsJsonAsync(
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