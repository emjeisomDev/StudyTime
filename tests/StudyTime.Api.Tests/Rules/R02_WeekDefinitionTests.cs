using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R02_WeekDefinitionTests : IAsyncLifetime
{
    private readonly DatabaseResetFixture _fixture;
    private readonly HttpClient _client;

    public R02_WeekDefinitionTests(DatabaseResetFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("2026-10-05T03:00:00Z", "2026-10-05")]
    [InlineData("2026-10-12T02:59:59Z", "2026-10-05")]
    [InlineData("2026-10-12T03:00:00Z", "2026-10-12")]
    public async Task Bootstrap_AtWeekBoundary_UsesSaoPauloWeekStart(string utcInstant, string expectedWeekStart)
    {
        _fixture.Factory.Clock.SetUtcNow(
            DateTime.Parse(
                utcInstant,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind));

        var studyAreaId = await CreateStudyAreaAsync("Matemática", 1500);
        var studyPlanId = await CreateStudyPlanAsync("Plano padrão", 1.00m);
        var response = await _client.PostAsJsonAsync("/bootstrap",
            new
            {
                items = new[]
                {
                    new { studyAreaId, studyPlanId }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var configuration = Assert.Single(document.RootElement.EnumerateArray());

        Assert.Equal(
            DateOnly.Parse(expectedWeekStart),
            DateOnly.Parse(configuration.GetProperty("weekStartDate").GetString()!));
    }

    private async Task<Guid> CreateStudyAreaAsync(string name, int standardWeeklyMinutes)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/study-areas",
            new
            {
                name,
                stdWeekStudyTime = standardWeeklyMinutes
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
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
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }
}
