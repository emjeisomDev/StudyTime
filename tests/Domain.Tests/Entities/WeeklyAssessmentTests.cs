using StudyTime.Domain.Entities;

namespace Domain.Tests.Entities;

public sealed class WeeklyAssessmentTests
{
    [Fact]
    public void CreateShouldGenerateValidAssessment()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        Assert.NotEqual(Guid.Empty, assessment.Id);
        Assert.Equal(2026, assessment.Year);
        Assert.Equal(36, assessment.WeekNumber);
        Assert.Equal(1500m, assessment.WeekGlobalGoal);
        Assert.Equal(0, assessment.MinutesStudied);
    }

    [Fact]
    public void CreateWithExplicitIdShouldPreserveId()
    {
        var id = Guid.NewGuid();

        var assessment = WeeklyAssessment.Create(id, 2026, 36, 1500m);

        Assert.Equal(id, assessment.Id);
    }

    [Fact]
    public void CreateShouldRejectEmptyId()
    {
        Assert.Throws<ArgumentException>(() =>
            WeeklyAssessment.Create(Guid.Empty, 2026, 36, 1500m));
    }

    [Fact]
    public void CreateShouldRejectNonPositiveYear()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(0, 36, 1500m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(-1, 36, 1500m));
    }

    [Fact]
    public void CreateShouldRejectInvalidWeekNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(2026, 0, 1500m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(2026, 54, 1500m));
    }

    [Fact]
    public void CreateShouldAcceptWeek53()
    {
        var assessment = WeeklyAssessment.Create(2026, 53, 1500m);

        Assert.Equal(53, assessment.WeekNumber);
    }

    [Fact]
    public void CreateShouldRejectNonPositiveGlobalGoal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(2026, 36, 0m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(2026, 36, -1m));
    }

    [Fact]
    public void CreateWithExplicitMinutesShouldAcceptZero()
    {
        var assessment = WeeklyAssessment.Create(Guid.NewGuid(), 2026, 36, 1500m, 0);

        Assert.Equal(0, assessment.MinutesStudied);
    }

    [Fact]
    public void CreateWithExplicitMinutesShouldRejectNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(Guid.NewGuid(), 2026, 36, 1500m, -1));
    }

    [Fact]
    public void UpdateGlobalGoalShouldUpdateValue()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        assessment.UpdateGlobalGoal(1800m);

        Assert.Equal(1800m, assessment.WeekGlobalGoal);
    }

    [Fact]
    public void UpdateGlobalGoalShouldRejectNonPositiveValue()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        Assert.Throws<ArgumentOutOfRangeException>(() => assessment.UpdateGlobalGoal(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => assessment.UpdateGlobalGoal(-1m));
    }

    [Fact]
    public void UpdateMinutesStudiedShouldUpdateValue()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        assessment.UpdateMinutesStudied(1200);

        Assert.Equal(1200, assessment.MinutesStudied);
    }

    [Fact]
    public void UpdateMinutesStudiedShouldRejectNegativeValue()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        Assert.Throws<ArgumentOutOfRangeException>(() => assessment.UpdateMinutesStudied(-1));
    }

    [Fact]
    public void IsGoalAchievedShouldBeFalseWhenCollectionIsEmpty()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        Assert.False(assessment.IsGoalAchieved(Array.Empty<StudyAreaWeekAssessment>()));
    }

    [Fact]
    public void IsGoalAchievedShouldBeTrueWhenAllAssessmentsAreAchieved()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var first = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 700m);
        var second = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 800m);

        first.UpdateMinutesStudied(700);
        second.UpdateMinutesStudied(800);

        Assert.True(assessment.IsGoalAchieved([first, second]));
    }

    [Fact]
    public void IsGoalAchievedShouldBeFalseWhenOneAssessmentIsNotAchieved()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var first = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 700m);
        var second = StudyAreaWeekAssessment.Create(Guid.NewGuid(), 800m);

        first.UpdateMinutesStudied(700);
        second.UpdateMinutesStudied(799);

        Assert.False(assessment.IsGoalAchieved([first, second]));
    }

    [Fact]
    public void IsGoalAchievedShouldRejectNullCollection()
    {
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);

        Assert.Throws<ArgumentNullException>(() => assessment.IsGoalAchieved(null!));
    }
}