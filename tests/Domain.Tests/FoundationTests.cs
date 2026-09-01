using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace Domain.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void DomainAssemblyShouldHaveExpectedName()
    {
        Assert.Equal("StudyTime.Domain", StudyTime.Domain.AssemblyMarker.Name);
    }

    [Fact]
    public void StudyAreaShouldRejectNonPositiveStandardWeeklyStudyTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyArea.Create("C#", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyArea.Create("C#", -1));
    }

    [Fact]
    public void StudyAreaShouldAcceptPositiveStandardWeeklyStudyTime()
    {
        var area = StudyArea.Create("C#", 600);

        Assert.Equal("C#", area.Name);
        Assert.Equal(600, area.StdWeekStudyTime);
    }

    [Fact]
    public void StudyPlanShouldAcceptOnlyPositiveCoefficient()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyPlan.Create("Intensivo", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyPlan.Create("Intensivo", -0.5m));

        var plan = StudyPlan.Create("Intensivo", 1.25m);

        Assert.Equal(1.25m, plan.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, plan.Status);
    }

    [Fact]
    public void StudyPlanShouldAllowActivationAndDeactivation()
    {
        var plan = StudyPlan.Create("Intensivo", 1m, StudyPlanStatus.Inactive);

        Assert.Equal(StudyPlanStatus.Inactive, plan.Status);

        plan.Activate();
        Assert.Equal(StudyPlanStatus.Active, plan.Status);

        plan.Deactivate();
        Assert.Equal(StudyPlanStatus.Inactive, plan.Status);
    }

    [Fact]
    public void StudyAreaWeekShouldRequireMondayAsWeekStart()
    {
        var area = StudyArea.Create("C#", 1000);
        var plan = StudyPlan.Create("Normal", 1m);
        var assessment = WeeklyAssessment.Create(2026, 35, 1000m);

        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 9, 1), area, plan, assessment.Id, 1000m));
    }

    [Fact]
    public void StudyAreaWeekShouldStorePreCalculatedIndividualGoal()
    {
        var area = StudyArea.Create("C#", 1000);
        var plan = StudyPlan.Create("Intensivo", 1.5m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);

        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area, plan, weeklyAssessment.Id, 1500m);

        Assert.Equal(1500m, week.Assessment.WeekIndividualGoal);
        Assert.Equal(0, week.Assessment.MinutesStudied);
        Assert.False(week.Assessment.GoalAchieved);
    }

    [Fact]
    public void StudyAreaWeekShouldRejectInactiveStudyPlan()
    {
        var area = StudyArea.Create("C#", 1000);
        var plan = StudyPlan.Create("Inativo", 1m, StudyPlanStatus.Inactive);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1000m);

        Assert.Throws<InvalidOperationException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area, plan, weeklyAssessment.Id, 1000m));
    }

    [Fact]
    public void IndividualAssessmentShouldBeAchievedWhenMinutesReachGoal()
    {
        var area = StudyArea.Create("C#", 1000);
        var plan = StudyPlan.Create("Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1000m);
        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area, plan, weeklyAssessment.Id, 1000m);

        week.Assessment.UpdateMinutesStudied(999);
        Assert.False(week.Assessment.GoalAchieved);

        week.Assessment.UpdateMinutesStudied(1000);
        Assert.True(week.Assessment.GoalAchieved);
    }

    [Fact]
    public void WeeklyAssessmentShouldAcceptPreCalculatedGlobalGoal()
    {
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1400m);

        Assert.Equal(1400m, weeklyAssessment.WeekGlobalGoal);
    }

    [Fact]
    public void WeeklyAssessmentShouldUpdateGlobalGoalWithCalculatedValue()
    {
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1000m);

        weeklyAssessment.UpdateGlobalGoal(1400m);

        Assert.Equal(1400m, weeklyAssessment.WeekGlobalGoal);
    }

    [Fact]
    public void WeeklyAssessmentShouldBeAchievedOnlyWhenAllIndividualGoalsAreAchieved()
    {
        var area1 = StudyArea.Create("C#", 1000);
        var area2 = StudyArea.Create("SQL", 500);
        var plan = StudyPlan.Create("Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);

        var week1 = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area1, plan, weeklyAssessment.Id, 1000m);
        var week2 = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area2, plan, weeklyAssessment.Id, 500m);

        week1.Assessment.UpdateMinutesStudied(1000);
        week2.Assessment.UpdateMinutesStudied(499);

        Assert.False(weeklyAssessment.IsGoalAchieved([week1.Assessment, week2.Assessment]));

        week2.Assessment.UpdateMinutesStudied(500);

        Assert.True(weeklyAssessment.IsGoalAchieved([week1.Assessment, week2.Assessment]));
    }

    [Fact]
    public void WeeklyAssessmentShouldStoreTheIsoYearAndWeekProvidedByTheApplication()
    {
        var assessment = WeeklyAssessment.Create(2026, 53, 1500m);

        Assert.Equal(2026, assessment.Year);
        Assert.Equal(53, assessment.WeekNumber);
    }

    [Fact]
    public void WeeklyAssessmentShouldRejectInvalidIsoWeek()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(2026, 0, 1500m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyAssessment.Create(2026, 54, 1500m));
    }

    [Fact]
    public void StudyRecordShouldRejectNonPositiveMinutes()
    {
        var weekStart = new DateOnly(2026, 8, 31);
        var weekId = Guid.NewGuid();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(new DateOnly(2026, 8, 31), 0, weekId, weekStart));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(new DateOnly(2026, 8, 31), -10, weekId, weekStart));
    }

    [Fact]
    public void StudyRecordShouldRejectDateOutsideConfiguredWeek()
    {
        var weekStart = new DateOnly(2026, 8, 31);
        var weekId = Guid.NewGuid();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(new DateOnly(2026, 8, 30), 60, weekId, weekStart));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 7), 60, weekId, weekStart));
    }

    [Fact]
    public void StudyRecordShouldAllowMultipleRecordsOnSameDay()
    {
        var weekStart = new DateOnly(2026, 8, 31);
        var weekId = Guid.NewGuid();

        var first = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero), 30, weekId, weekStart);
        var second = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero), 45, weekId, weekStart);

        Assert.Equal(first.Date, second.Date);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void StudyRecordLifoShouldSelectNewestCreatedAt()
    {
        var weekStart = new DateOnly(2026, 8, 31);
        var weekId = Guid.NewGuid();

        var oldest = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero), 30, weekId, weekStart);
        var newest = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero), 45, weekId, weekStart);

        var selected = StudyRecord.SelectLastForDeletion([oldest, newest]);

        Assert.Equal(newest.Id, selected.Id);
    }

    [Fact]
    public void StudyRecordLifoShouldUseIdAsDeterministicTieBreaker()
    {
        var weekStart = new DateOnly(2026, 8, 31);
        var weekId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var first = StudyRecord.Create(lowerId, new DateOnly(2026, 9, 1), createdAt, 30, weekId, weekStart);
        var second = StudyRecord.Create(higherId, new DateOnly(2026, 9, 1), createdAt, 45, weekId, weekStart);
        var selected = StudyRecord.SelectLastForDeletion([first, second]);

        Assert.Equal(higherId, selected.Id);
    }

    [Fact]
    public void StudyRecordLifoShouldRejectEmptyCollection()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StudyRecord.SelectLastForDeletion(Array.Empty<StudyRecord>()));
    }

    [Fact]
    public void StudyAreaWeekShouldReconfigureWithPreCalculatedIndividualGoal()
    {
        var originalArea = StudyArea.Create("C#", 1000);
        var newArea = StudyArea.Create("SQL", 1200);
        var plan = StudyPlan.Create("Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1000m);

        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), originalArea, plan, weeklyAssessment.Id, 1000m);

        Assert.Equal(1000m, week.Assessment.WeekIndividualGoal);

        week.Reconfigure(newArea, plan, 1200m);

        Assert.Equal(newArea.Id, week.StudyAreaId);
        Assert.Equal(1200m, week.Assessment.WeekIndividualGoal);
    }
}