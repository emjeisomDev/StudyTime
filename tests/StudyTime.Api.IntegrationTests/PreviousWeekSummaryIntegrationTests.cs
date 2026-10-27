using Xunit;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.IntegrationTests.Support;
using StudyTime.Api.IntegrationTests.Fixtures;

namespace StudyTime.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class PreviousWeekSummaryIntegrationTests : ApiIntegrationTestBase
{
    public PreviousWeekSummaryIntegrationTests(ApiIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task ConsultarResumoDaSemanaAnterior_SemDados_Retorna404ProblemDetails()
    {
        // Arrange: the per-test fixture reset leaves the database without weeks.

        // Act
        using HttpResponseMessage response =
            await Client.GetAsync("/weeks/previous-summary");

        // Assert
        using JsonDocument problem =
            await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);

        string detail = problem.RootElement.GetProperty("detail").GetString()!;
        Assert.Contains("ISO week", detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("was not found", detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConsultarResumoDaSemanaAnterior_ComConfiguracao_RetornaEstruturaCompleta()
    {
        // Arrange
        await Fixture.SeedPreviousWeekAsync(includeStudyRecord: false);

        // Act
        using HttpResponseMessage response =
            await Client.GetAsync("/weeks/previous-summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PreviousWeekSummaryApiResponse? summary =
            await response.Content.ReadFromJsonAsync<PreviousWeekSummaryApiResponse>();

        Assert.NotNull(summary);
        Assert.Equal(
            ApiTestDateHelper.GetCurrentWeekStart().AddDays(-7),
            summary.WeekStartDate);
        Assert.Equal(1500m, summary.WeeklyAssessment.WeekGlobalGoal);
        Assert.NotEqual(Guid.Empty, summary.WeeklyAssessment.Id);

        PreviousWeekSummaryItemApiResponse item = Assert.Single(summary.Items);
        Assert.Equal("Área da semana anterior", item.StudyArea.Name);
        Assert.Equal(600, item.StudyArea.StdWeekStudyTime);
        Assert.Equal("Plano da semana anterior", item.StudyPlan.Name);
        Assert.Equal("Active", item.StudyPlan.Status, ignoreCase: true);
        Assert.Equal(600m, item.WeekIndividualGoal);
        Assert.Equal(600, item.MinutesStudied);
        Assert.True(item.GoalAchieved);
    }
}
