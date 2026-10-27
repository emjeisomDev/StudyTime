using Xunit;
using System.Net;
using System.Net.Http.Json;
using StudyTime.Api.IntegrationTests.Support;
using StudyTime.Api.IntegrationTests.Fixtures;

namespace StudyTime.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class StudyTimeApiEndToEndTests : ApiIntegrationTestBase
{
    public StudyTimeApiEndToEndTests(ApiIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task FluxoCompleto_CriaConfiguracaoRegistraEstudoAvancaSemanaEDesfazUltimoRegistro()
    {
        // Arrange
        StudyAreaApiResponse mathematics =
            await CreateStudyAreaAsync("Matemática", 600);
        StudyAreaApiResponse physics =
            await CreateStudyAreaAsync("Física", 900);
        StudyPlanApiResponse plan =
            await CreateStudyPlanAsync("Plano principal", 1.0m);

        Assert.Equal("Active", plan.Status, ignoreCase: true);

        // Act: bootstrap the current week with both areas (600 + 900 = 1500).
        using HttpResponseMessage bootstrapResponse = await PostBootstrapAsync(
            (mathematics.Id, plan.Id),
            (physics.Id, plan.Id));

        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        List<StudyAreaWeekApiResponse> bootstrapped =
            await bootstrapResponse.Content
                .ReadFromJsonAsync<List<StudyAreaWeekApiResponse>>()
            ?? throw new InvalidOperationException(
                "The bootstrap response did not contain weekly configurations.");

        Assert.Equal(2, bootstrapped.Count);

        StudyAreaWeekApiResponse mathematicsWeek =
            Assert.Single(bootstrapped, item => item.StudyAreaId == mathematics.Id);
        StudyAreaWeekApiResponse physicsWeek =
            Assert.Single(bootstrapped, item => item.StudyAreaId == physics.Id);

        Assert.Equal(600m, mathematicsWeek.WeekIndividualGoal);
        Assert.Equal(900m, physicsWeek.WeekIndividualGoal);
        Assert.False(mathematicsWeek.GoalAchieved);
        Assert.False(physicsWeek.GoalAchieved);

        using (HttpResponseMessage getMathematicsWeek = await Client.GetAsync(
            $"/study-area-weeks/{mathematicsWeek.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, getMathematicsWeek.StatusCode);
            StudyAreaWeekApiResponse response =
                await getMathematicsWeek.Content
                    .ReadFromJsonAsync<StudyAreaWeekApiResponse>()
                ?? throw new InvalidOperationException(
                    "The StudyAreaWeek response was empty.");

            Assert.Equal(600m, response.WeekIndividualGoal);
            Assert.Equal(0, response.MinutesStudied);
        }

        using (HttpResponseMessage getPhysicsWeek = await Client.GetAsync(
            $"/study-area-weeks/{physicsWeek.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, getPhysicsWeek.StatusCode);
            StudyAreaWeekApiResponse response =
                await getPhysicsWeek.Content
                    .ReadFromJsonAsync<StudyAreaWeekApiResponse>()
                ?? throw new InvalidOperationException(
                    "The StudyAreaWeek response was empty.");

            Assert.Equal(900m, response.WeekIndividualGoal);
            Assert.Equal(0, response.MinutesStudied);
        }

        // Each area gets one initial 60-minute record, then additional records
        // for the remaining 540 and 840 minutes respectively.
        await AddStudyRecordAsync(mathematicsWeek.Id, 60);
        await AddStudyRecordAsync(physicsWeek.Id, 60);

        for (int index = 0; index < 9; index++)
        {
            await AddStudyRecordAsync(mathematicsWeek.Id, 60);
        }

        for (int index = 0; index < 14; index++)
        {
            await AddStudyRecordAsync(physicsWeek.Id, 60);
        }

        IReadOnlyList<StudyRecordApiResponse> mathematicsRecords =
            await GetStudyRecordsAsync(mathematicsWeek.Id);
        IReadOnlyList<StudyRecordApiResponse> physicsRecords =
            await GetStudyRecordsAsync(physicsWeek.Id);

        Assert.Equal(10, mathematicsRecords.Count);
        Assert.Equal(15, physicsRecords.Count);
        Assert.All(mathematicsRecords, record => Assert.Equal(60, record.Minutes));
        Assert.All(physicsRecords, record => Assert.Equal(60, record.Minutes));
        Assert.Single(mathematicsRecords.Select(record => record.Date).Distinct());
        Assert.Single(physicsRecords.Select(record => record.Date).Distinct());
        AssertRecordsOrderedByCreatedAt(mathematicsRecords);
        AssertRecordsOrderedByCreatedAt(physicsRecords);

        StudyAreaWeekApiResponse mathematicsAfterGoal =
            await GetStudyAreaWeekAsync(mathematicsWeek.Id);
        StudyAreaWeekApiResponse physicsAfterGoal =
            await GetStudyAreaWeekAsync(physicsWeek.Id);

        Assert.Equal(600, mathematicsAfterGoal.MinutesStudied);
        Assert.True(mathematicsAfterGoal.GoalAchieved);
        Assert.Equal(900, physicsAfterGoal.MinutesStudied);
        Assert.True(physicsAfterGoal.GoalAchieved);

        // The individual endpoint would violate the 1500-minute global minimum
        // for a single area. Batch creation keeps both areas in the new week.
        DateOnly nextWeekStart = mathematicsWeek.WeekStartDate.AddDays(7);
        using HttpResponseMessage nextWeekResponse =
            await Client.PostAsJsonAsync(
                "/study-area-weeks/batch",
                new
                {
                    weekStartDate = nextWeekStart,
                    items = new[]
                    {
                        new
                        {
                            studyAreaId = mathematics.Id,
                            studyPlanId = plan.Id
                        },
                        new
                        {
                            studyAreaId = physics.Id,
                            studyPlanId = plan.Id
                        }
                    }
                });

        Assert.Equal(HttpStatusCode.Created, nextWeekResponse.StatusCode);

        List<StudyAreaWeekApiResponse> nextWeek =
            await nextWeekResponse.Content
                .ReadFromJsonAsync<List<StudyAreaWeekApiResponse>>()
            ?? throw new InvalidOperationException(
                "The next-week response did not contain weekly configurations.");

        Assert.Equal(2, nextWeek.Count);
        Assert.All(nextWeek, item => Assert.Equal(nextWeekStart, item.WeekStartDate));

        // Delete the last record from Mathematics and verify its aggregate changes.
        using HttpResponseMessage deleteLastResponse = await Client.DeleteAsync(
            $"/study-area-weeks/{mathematicsWeek.Id}/study-records/last");

        Assert.Equal(HttpStatusCode.NoContent, deleteLastResponse.StatusCode);

        StudyAreaWeekApiResponse mathematicsAfterDeletion =
            await GetStudyAreaWeekAsync(mathematicsWeek.Id);

        Assert.Equal(540, mathematicsAfterDeletion.MinutesStudied);
        Assert.False(mathematicsAfterDeletion.GoalAchieved);
        Assert.Equal(9, (await GetStudyRecordsAsync(mathematicsWeek.Id)).Count);
    }

    private async Task AddStudyRecordAsync(Guid studyAreaWeekId, int minutes)
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            $"/study-area-weeks/{studyAreaWeekId}/study-records",
            new { minutes });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        StudyRecordApiResponse? record =
            await response.Content.ReadFromJsonAsync<StudyRecordApiResponse>();

        Assert.NotNull(record);
        Assert.Equal(studyAreaWeekId, record.StudyAreaWeekId);
        Assert.Equal(minutes, record.Minutes);
    }

    private async Task<IReadOnlyList<StudyRecordApiResponse>> GetStudyRecordsAsync(
        Guid studyAreaWeekId)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            $"/study-area-weeks/{studyAreaWeekId}/study-records");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content
            .ReadFromJsonAsync<List<StudyRecordApiResponse>>()
            ?? throw new InvalidOperationException(
                "The StudyRecord list response was empty.");
    }

    private async Task<StudyAreaWeekApiResponse> GetStudyAreaWeekAsync(Guid id)
    {
        using HttpResponseMessage response =
            await Client.GetAsync($"/study-area-weeks/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<StudyAreaWeekApiResponse>()
            ?? throw new InvalidOperationException(
                "The StudyAreaWeek response was empty.");
    }

    private static void AssertRecordsOrderedByCreatedAt(
        IReadOnlyList<StudyRecordApiResponse> records)
    {
        Assert.True(
            records.Zip(
                    records.Skip(1),
                    (previous, next) => previous.CreatedAt <= next.CreatedAt)
                .All(isOrdered => isOrdered));
    }
}