using StudyTime.Domain.Entities;

namespace Domain.Tests.Entities;

public sealed class StudyAreaWeekAssessmentTests
{
    [Fact]
    public void CreateShouldGenerateValidAssessment()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var assessment = StudyAreaWeekAssessment.Create(studyAreaWeekId, 1500m);

        Assert.NotEqual(Guid.Empty, assessment.Id);
        Assert.Equal(studyAreaWeekId, assessment.StudyAreaWeekId);
        Assert.Equal(1500m, assessment.WeekIndividualGoal);
        Assert.Equal(0, assessment.MinutesStudied);
        Assert.False(assessment.GoalAchieved);
    }

    [Fact]
    public void CreateWithExplicitIdShouldPreserveValues()
    {
        var id = Guid.NewGuid();
        var studyAreaWeekId = Guid.NewGuid();
        var assessment = StudyAreaWeekAssessment.Create(id, studyAreaWeekId, 1500m, 1200);

        Assert.Equal(id, assessment.Id);
        Assert.Equal(studyAreaWeekId, assessment.StudyAreaWeekId);
        Assert.Equal(1500m, assessment.WeekIndividualGoal);
        Assert.Equal(1200, assessment.MinutesStudied);
        Assert.False(assessment.GoalAchieved);
    }

    [Fact]
    public void CreateShouldRejectEmptyAssessmentId()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeekAssessment.Create(Guid.Empty, Guid.NewGuid(), 1500m));
    }

    [Fact]
    public void CreateShouldRejectEmptyStudyAreaWeekId()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeekAssessment.Create(Guid.NewGuid(), Guid.Empty, 1500m));
    }

    [Fact]
    public void CreateShouldRejectNonPositiveGoal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyAreaWeekAssessment.Create(Guid.NewGuid(), 0m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyAreaWeekAssessment.Create(Guid.NewGuid(), -1m));
    }

    [Fact]
    public void CreateWithExplicitMinutesShouldRejectNegativeMinutes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyAreaWeekAssessment.Create(Guid.NewGuid(), Guid.NewGuid(), 1500m, -1));
    }

    [Fact]
    public void UpdateMinutesStudiedShouldUpdateValue()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 1500m);

        assessment.UpdateMinutesStudied(1500);

        Assert.Equal(1500, assessment.MinutesStudied);
        Assert.True(assessment.GoalAchieved);
    }

    [Fact]
    public void UpdateMinutesStudiedShouldRejectNegativeValue()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 1500m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            assessment.UpdateMinutesStudied(-1));
    }

    [Fact]
    public void GoalAchievedShouldBeTrueWhenMinutesReachGoal()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 1500m);

        assessment.UpdateMinutesStudied(1500);

        Assert.True(assessment.GoalAchieved);
    }

    [Fact]
    public void GoalAchievedShouldBeTrueWhenMinutesExceedGoal()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 1500m);

        assessment.UpdateMinutesStudied(1600);

        Assert.True(assessment.GoalAchieved);
    }

    [Fact]
    public void GoalAchievedShouldBeFalseWhenMinutesAreBelowGoal()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 1500m);

        assessment.UpdateMinutesStudied(1499);

        Assert.False(assessment.GoalAchieved);
    }

    [Fact]
    public void RecalculateGoalShouldUpdateIndividualGoal()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 600m);

        assessment.RecalculateGoal(1500m);

        Assert.Equal(1500m, assessment.WeekIndividualGoal);
    }

    [Fact]
    public void RecalculateGoalShouldRejectZeroGoal()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 600m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            assessment.RecalculateGoal(0m));
    }

    [Fact]
    public void RecalculateGoalShouldRejectNegativeGoal()
    {
        var assessment = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 600m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            assessment.RecalculateGoal(-1m));
    }
}