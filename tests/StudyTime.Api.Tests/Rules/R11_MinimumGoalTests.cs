using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R11_MinimumGoalTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public R11_MinimumGoalTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(1499)]
    [InlineData(1000)]
    public async Task CreateBatch_GlobalGoalBelow1500_Returns422AndDoesNotPersist(
        int standardWeeklyMinutes)
    {
        var studyAreaId = await CreateStudyAreaAsync("Matemática", standardWeeklyMinutes);
        var planId = await CreateStudyPlanAsync("Plano padrão", 1.00m);
        var baselineAreaId = await CreateStudyAreaAsync("Área de referência", 1500);

        // R08 exige que a meta global da semana corrente seja atingida antes
        // de o endpoint batch validar a configuração de uma semana futura.
        var bootstrapResponse = await _client.PostAsJsonAsync(
            "/bootstrap",
            new
            {
                items = new[]
                {
                    new { studyAreaId = baselineAreaId, studyPlanId = planId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        using var bootstrapDocument = JsonDocument.Parse(
            await bootstrapResponse.Content.ReadAsStringAsync());
        var currentConfiguration = Assert.Single(
            bootstrapDocument.RootElement.EnumerateArray());
        var currentStudyAreaWeekId = currentConfiguration.GetProperty("id").GetGuid();
        var currentWeekStart = DateOnly.Parse(
            currentConfiguration.GetProperty("weekStartDate").GetString()!);
        var targetWeekStart = currentWeekStart.AddDays(7);

        var recordResponse = await _client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{currentStudyAreaWeekId}/study-records",
            new { minutes = 1500 });

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = targetWeekStart,
                items = new[]
                {
                    new { studyAreaId, studyPlanId = planId }
                }
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using (var problem = JsonDocument.Parse(
                   await response.Content.ReadAsStringAsync()))
        {
            Assert.Equal(422, problem.RootElement.GetProperty("status").GetInt32());
            Assert.Equal(
                "Unprocessable Entity",
                problem.RootElement.GetProperty("title").GetString());
            Assert.False(string.IsNullOrWhiteSpace(
                problem.RootElement.GetProperty("detail").GetString()));
        }

        // Tenta novamente com a mesma área e a mesma semana, agora com um
        // coeficiente que torna a meta válida. O sucesso comprova que a
        // tentativa rejeitada não persistiu uma StudyAreaWeek parcialmente.
        var validCoefficient = decimal.Ceiling(
            1500m / standardWeeklyMinutes * 100m) / 100m;
        var validPlanId = await CreateStudyPlanAsync(
            "Plano com meta válida",
            validCoefficient);

        var retryResponse = await _client.PostAsJsonAsync(
            "/api/v1/study-area-weeks/batch",
            new
            {
                weekStartDate = targetWeekStart,
                items = new[]
                {
                    new { studyAreaId, studyPlanId = validPlanId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
        using var result = JsonDocument.Parse(
            await retryResponse.Content.ReadAsStringAsync());
        var configuration = Assert.Single(result.RootElement.EnumerateArray());
        Assert.Equal(
            decimal.Round(standardWeeklyMinutes * validCoefficient, 2),
            configuration.GetProperty("weekIndividualGoal").GetDecimal());
        Assert.Equal(
            targetWeekStart,
            DateOnly.Parse(configuration.GetProperty("weekStartDate").GetString()!));
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
