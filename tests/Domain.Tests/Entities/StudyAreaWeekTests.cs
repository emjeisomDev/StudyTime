using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace Domain.Tests.Entities;

public sealed class StudyAreaWeekTests
{
    private static StudyArea CreateArea(int minutes = 1000)
        => StudyArea.Create(Guid.NewGuid(), "C#", minutes);

    private static StudyPlan CreatePlan(decimal coefficient = 1.5m)
        => StudyPlan.Create(Guid.NewGuid(), "Intensivo", coefficient);

    [Fact]
    public void CreateShouldGenerateValidStudyAreaWeek()
    {
        var area = CreateArea();
        var plan = CreatePlan();
        var weeklyAssessmentId = Guid.NewGuid();

        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area, plan, weeklyAssessmentId, 1500m);

        Assert.NotEqual(Guid.Empty, week.Id);
        Assert.Equal(new DateOnly(2026, 8, 31), week.WeekStartDate);
        Assert.Equal(area.Id, week.StudyAreaId);
        Assert.Equal(plan.Id, week.StudyPlanId);
        Assert.Equal(weeklyAssessmentId, week.WeeklyAssessmentId);
        Assert.NotNull(week.Assessment);
        Assert.Equal(week.Id, week.Assessment.StudyAreaWeekId);
        Assert.Equal(1500m, week.Assessment.WeekIndividualGoal);
    }

    [Fact]
    public void CreateShouldUseProvidedIndividualGoal()
    {
        var area = CreateArea(1000);
        var plan = CreatePlan(1.5m);

        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), area, plan, Guid.NewGuid(), 1500m);

        Assert.Equal(1500m, week.Assessment.WeekIndividualGoal);
        Assert.Equal(0, week.Assessment.MinutesStudied);
        Assert.False(week.Assessment.GoalAchieved);
    }

    [Fact]
    public void CreateWithExplicitIdShouldPreserveId()
    {
        var id = Guid.NewGuid();

        var week = StudyAreaWeek.Create(id, new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(), Guid.NewGuid(), 1500m);

        Assert.Equal(id, week.Id);
        Assert.Equal(id, week.Assessment.StudyAreaWeekId);
    }

    [Fact]
    public void CreateShouldRejectEmptyId()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(Guid.Empty, new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(), Guid.NewGuid(), 1500m));
    }

    [Fact]
    public void CreateShouldRejectNonMondayWeekStartDate()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 9, 1), CreateArea(), CreatePlan(), Guid.NewGuid(), 1500m));
    }

    [Fact]
    public void CreateShouldRejectNullStudyArea()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 8, 31), null!, CreatePlan(), Guid.NewGuid(), 1500m));
    }

    [Fact]
    public void CreateShouldRejectNullStudyPlan()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), null!, Guid.NewGuid(), 1500m));
    }

    [Fact]
    public void CreateShouldRejectInactiveStudyPlan()
    {
        var inactivePlan = StudyPlan.Create(Guid.NewGuid(), "Inativo", 1m, StudyPlanStatus.Inactive);

        Assert.Throws<InvalidOperationException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), inactivePlan, Guid.NewGuid(), 1000m));
    }

    [Fact]
    public void CreateShouldRejectEmptyWeeklyAssessmentId()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(), Guid.Empty, 1500m));
    }

    [Fact]
    public void ReconfigureShouldUpdateAreaPlanAndGoal()
    {
        var originalArea = CreateArea(1000);
        var originalPlan = CreatePlan(1m);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Mathematics", 1200);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Advanced", 1.25m);
        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), originalArea, originalPlan, Guid.NewGuid(), 1000m);

        week.Reconfigure(newArea, newPlan, 1500m);

        Assert.Equal(newArea.Id, week.StudyAreaId);
        Assert.Equal(newPlan.Id, week.StudyPlanId);
        Assert.Equal(1500m, week.Assessment.WeekIndividualGoal);
    }

    [Fact]
    public void ReconfigureShouldPreserveAssessmentIdentity()
    {
        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(1m), Guid.NewGuid(), 1000m);
        var assessmentId = week.Assessment.Id;

        week.Reconfigure(CreateArea(1200), CreatePlan(1.25m), 1500m);

        Assert.Equal(assessmentId, week.Assessment.Id);
    }

    [Fact]
    public void ReconfigureShouldRejectNullStudyArea()
    {
        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(), Guid.NewGuid(), 1500m);

        Assert.Throws<ArgumentNullException>(() => week.Reconfigure(null!, CreatePlan(), 1500m));
    }

    [Fact]
    public void ReconfigureShouldRejectNullStudyPlan()
    {
        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(), Guid.NewGuid(), 1500m);

        Assert.Throws<ArgumentNullException>(() => week.Reconfigure(CreateArea(), null!, 1500m));
    }

    [Fact]
    public void ReconfigureShouldRejectInactiveStudyPlan()
    {
        var week = StudyAreaWeek.Create(new DateOnly(2026, 8, 31), CreateArea(), CreatePlan(), Guid.NewGuid(), 1500m);
        var inactivePlan = StudyPlan.Create(Guid.NewGuid(), "Inativo", 1m, StudyPlanStatus.Inactive);

        Assert.Throws<InvalidOperationException>(() => week.Reconfigure(CreateArea(), inactivePlan, 1500m));
    }
}