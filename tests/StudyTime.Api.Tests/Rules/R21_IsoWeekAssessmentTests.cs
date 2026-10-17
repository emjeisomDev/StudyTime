using System.Net;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R21_IsoWeekAssessmentTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private ApiWebApplicationFactory Factory => _fixture.Factory;

    public R21_IsoWeekAssessmentTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        // 2020-12-28 é segunda-feira da semana ISO 2020-W53.
        SetClock("2020-12-28T15:00:00Z");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Bootstrap_WhenAreasBelongToSameIsoWeek_ReusesOneWeeklyAssessment()
    {
        using var client = Factory.CreateClient();

        var firstAreaId = await CreateStudyAreaAsync(
            client,
            "R21 - Área A",
            750);

        var secondAreaId = await CreateStudyAreaAsync(
            client,
            "R21 - Área B",
            750);

        var studyPlanId = await CreateStudyPlanAsync(
            client,
            "R21 - Plano",
            1.0m);

        // R08/R08A: a primeira configuração deve ser criada pelo bootstrap.
        using var bootstrapResponse = await client.PostAsJsonAsync(
            "/bootstrap",
            new
            {
                items = new[]
                {
                    new { studyAreaId = firstAreaId, studyPlanId },
                    new { studyAreaId = secondAreaId, studyPlanId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        using var bootstrapDocument = JsonDocument.Parse(
            await bootstrapResponse.Content.ReadAsStringAsync());

        var configurations = bootstrapDocument.RootElement
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, configurations.Length);

        var assessmentIds = configurations
            .Select(item => item.GetProperty("weeklyAssessmentId").GetGuid())
            .Distinct()
            .ToArray();

        Assert.Single(assessmentIds);

        Assert.All(
            configurations,
            item => Assert.Equal(
                new DateOnly(2020, 12, 28),
                DateOnly.Parse(
                    item.GetProperty("weekStartDate").GetString()!,
                    CultureInfo.InvariantCulture)));

        // Avançar uma semana torna 2020-W53 a semana anterior.
        SetClock("2021-01-04T15:00:00Z");

        using var summaryResponse = await client.GetAsync(
            "/weeks/previous-summary");

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);

        using var summaryDocument = JsonDocument.Parse(
            await summaryResponse.Content.ReadAsStringAsync());

        var weeklyAssessment = summaryDocument.RootElement
            .GetProperty("weeklyAssessment");

        Assert.Equal(2020, weeklyAssessment.GetProperty("year").GetInt32());
        Assert.Equal(53, weeklyAssessment.GetProperty("weekNumber").GetInt32());

        Assert.Equal(
            assessmentIds[0],
            weeklyAssessment.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task CreateBatch_ForSameIsoWeek_ReusesAssessmentAcrossRequests()
    {
        using var client = Factory.CreateClient();

        // Esta segunda prova usa a semana corrente de 2026, não a semana ISO
        // 2020-W53 definida como padrão no InitializeAsync.
        SetClock("2026-10-05T15:00:00Z");

        var firstAreaId = await CreateStudyAreaAsync(
            client,
            "R21 - Área inicial",
            1500);

        var secondAreaId = await CreateStudyAreaAsync(
            client,
            "R21 - Área adicional",
            300);

        var studyPlanId = await CreateStudyPlanAsync(
            client,
            "R21 - Plano compartilhado",
            1.0m);

        // R08/R08A: cria a configuração inicial por bootstrap.
        using var bootstrapResponse = await client.PostAsJsonAsync(
            "/bootstrap",
            new
            {
                items = new[]
                {
                    new
                    {
                        studyAreaId = firstAreaId,
                        studyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        using var firstDocument = JsonDocument.Parse(
            await bootstrapResponse.Content.ReadAsStringAsync());

        var firstStudyAreaWeek = firstDocument.RootElement[0];
        var firstStudyAreaWeekId = firstStudyAreaWeek
            .GetProperty("id")
            .GetGuid();
        var firstAssessmentId = firstStudyAreaWeek
            .GetProperty("weeklyAssessmentId")
            .GetGuid();

        // R08: a alteração manual é permitida somente após atingir a meta atual.
        using var recordResponse = await client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{firstStudyAreaWeekId}/study-records",
            new { minutes = 1500 });

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);

        using var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = "2026-10-05",
                items = new[]
                {
                    new
                    {
                        studyAreaId = secondAreaId,
                        studyPlanId
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        using var secondDocument = JsonDocument.Parse(
            await secondResponse.Content.ReadAsStringAsync());

        var secondAssessmentId = secondDocument.RootElement[0]
            .GetProperty("weeklyAssessmentId")
            .GetGuid();

        Assert.Equal(firstAssessmentId, secondAssessmentId);
    }

    private void SetClock(string utcInstant)
    {
        Factory.Clock.SetUtcNow(
            DateTime.Parse(
                utcInstant,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind));
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