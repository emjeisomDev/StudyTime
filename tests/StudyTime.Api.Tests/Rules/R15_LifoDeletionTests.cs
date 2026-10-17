using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.Tests.Fixtures;

namespace StudyTime.Api.Tests.Rules;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class R15_LifoDeletionTests : IAsyncLifetime
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly DatabaseResetFixture _databaseResetFixture;

    public R15_LifoDeletionTests(DatabaseResetFixture databaseResetFixture)
    {
        _databaseResetFixture = databaseResetFixture;
        _factory = databaseResetFixture.Factory;
    }

    public async Task InitializeAsync()
    {
        await _databaseResetFixture.ResetAsync();
        _factory.Clock.SetUtcNow(
            DateTime.Parse(
                "2026-10-05T15:00:00Z",
                null,
                System.Globalization.DateTimeStyles.RoundtripKind));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteLast_WhenMultipleRecordsExist_RemovesNewestCreatedRecord()
    {
        using var client = _factory.CreateClient();

        var (studyAreaWeekId, _, _) = await CreateValidConfigurationAsync(client);

        var firstRecord = await CreateRecordAsync(client, studyAreaWeekId, 10);
        await Task.Delay(30);
        var secondRecord = await CreateRecordAsync(client, studyAreaWeekId, 20);
        await Task.Delay(30);
        var thirdRecord = await CreateRecordAsync(client, studyAreaWeekId, 30);

        Assert.True(
            firstRecord.CreatedAt <= secondRecord.CreatedAt,
            "The second record should not be created before the first.");

        Assert.True(
            secondRecord.CreatedAt <= thirdRecord.CreatedAt,
            "The third record should not be created before the second.");

        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records/last");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getResponse = await client.GetAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var recordsDocument = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        var remainingRecords = recordsDocument.RootElement
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, remainingRecords.Length);

        var remainingIds = remainingRecords
            .Select(record => record.GetProperty("id").GetGuid())
            .ToHashSet();

        Assert.Contains(firstRecord.Id, remainingIds);
        Assert.Contains(secondRecord.Id, remainingIds);
        Assert.DoesNotContain(thirdRecord.Id, remainingIds);
    }

    [Fact]
    public async Task DeleteLast_WhenCreatedAtTies_RemovesRecordWithGreatestId()
    {
        using var client = _factory.CreateClient();

        var (studyAreaWeekId, _, _) = await CreateValidConfigurationAsync(client);

        var firstRecord = await CreateRecordAsync(client, studyAreaWeekId, 15);
        var secondRecord = await CreateRecordAsync(client, studyAreaWeekId, 25);

        if (firstRecord.CreatedAt != secondRecord.CreatedAt)
        {
            return;
        }

        var expectedRemovedId = firstRecord.Id.CompareTo(secondRecord.Id) > 0
            ? firstRecord.Id
            : secondRecord.Id;

        using var deleteResponse = await client.DeleteAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records/last");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getResponse = await client.GetAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var recordsDocument = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        var remainingIds = recordsDocument.RootElement
            .EnumerateArray()
            .Select(record => record.GetProperty("id").GetGuid())
            .ToHashSet();

        Assert.DoesNotContain(expectedRemovedId, remainingIds);
        Assert.Single(remainingIds);
    }

    private static async Task<(Guid StudyAreaWeekId, Guid StudyAreaId, Guid StudyPlanId)>
        CreateValidConfigurationAsync(HttpClient client)
    {
        var studyAreaId = await CreateStudyAreaAsync(client, "R15 - Área de estudo", 1500);
        var studyPlanId = await CreateStudyPlanAsync(client, "R15 - Plano de estudo", 1.0m);

        using var bootstrapResponse = await client.PostAsJsonAsync("/bootstrap",
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
        using var bootstrapDocument = JsonDocument.Parse(await bootstrapResponse.Content.ReadAsStringAsync());
        var studyAreaWeekId = bootstrapDocument.RootElement[0].GetProperty("id").GetGuid();

        return (studyAreaWeekId, studyAreaId, studyPlanId);
    }

    private static async Task<(Guid Id, DateTime CreatedAt)> CreateRecordAsync(
        HttpClient client,
        Guid studyAreaWeekId,
        int minutes)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = document.RootElement;

        return (record.GetProperty("id").GetGuid(), record.GetProperty("createdAt").GetDateTime());
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

    private static async Task<Guid> CreateStudyPlanAsync(
        HttpClient client,
        string name,
        decimal coefficient)
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