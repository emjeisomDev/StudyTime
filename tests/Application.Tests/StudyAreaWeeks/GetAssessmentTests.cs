using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

public sealed class GetAssessmentTests
{
    [Fact]
    public async Task GetAssessmentShouldReturnIndividualAssessment()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var week = new DateOnly(2026, 9, 7);
        var assessmentId = Guid.NewGuid();
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, area, plan, assessmentId, 1000m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1000);

        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([studyAreaWeek]));

        var result = await service.GetAssessmentAsync(studyAreaWeek.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(studyAreaWeek.Id, result.StudyAreaWeekId);
        Assert.Equal(1000m, result.WeekIndividualGoal);
        Assert.Equal(1000, result.MinutesStudied);
        Assert.True(result.GoalAchieved);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnFalseWhenIndividualGoalIsNotAchieved()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), new DateOnly(2026, 9, 7), area, plan, Guid.NewGuid(), 1000m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(999);

        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([studyAreaWeek]));

        var result = await service.GetAssessmentAsync(studyAreaWeek.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1000m, result.WeekIndividualGoal);
        Assert.Equal(999, result.MinutesStudied);
        Assert.False(result.GoalAchieved);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnNullWhenStudyAreaWeekDoesNotExist()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));
        var result = await service.GetAssessmentAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssessmentShouldRejectEmptyStudyAreaWeekId()
    {
        var service = TestHelpers.CreateService(new FakeStudyAreaWeekRepository([]));
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAssessmentAsync(Guid.Empty, CancellationToken.None));
        Assert.Contains("StudyAreaWeekId", exception.Message);
    }
}