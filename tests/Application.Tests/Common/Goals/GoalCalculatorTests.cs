using StudyTime.Application.Common.Goals;
using StudyTime.Domain.Entities;

namespace Application.Tests.Common.Goals;

public sealed class GoalCalculatorTests
{
    private static readonly decimal[] StandardGoals = [600m, 450m, 500m];
    private static readonly decimal[] DecimalGoals = [337.5m, 412.25m, 500.25m];
    private static readonly decimal[] BelowMinimumGoals = [400m, 500m];
    private static readonly decimal[] InvalidGoals = [500m, 0m, 500m];
    private readonly GoalCalculator _calculator = new();

    [Fact]
    public void ShouldCalculateIndividualGoal()
    {
        var studyArea = StudyArea.Create(Guid.NewGuid(), "Mathematics", 600);
        var studyPlan = StudyPlan.Create(Guid.NewGuid(), "Standard", 1.5m);

        var result = _calculator.CalculateIndividualGoal(studyArea, studyPlan);

        Assert.Equal(900m, result);
    }

    [Fact]
    public void ShouldCalculateIndividualGoalWithFractionalCoefficient()
    {
        var studyArea = StudyArea.Create(Guid.NewGuid(), "Physics", 1000);
        var studyPlan = StudyPlan.Create(Guid.NewGuid(), "Advanced", 0.75m);

        var result = _calculator.CalculateIndividualGoal(studyArea, studyPlan);

        Assert.Equal(750m, result);
    }

    [Fact]
    public void ShouldCalculateGlobalGoalFromIndividualGoals()
    {
        var result = _calculator.CalculateGlobalGoal(StandardGoals);

        Assert.Equal(1550m, result);
    }

    [Fact]
    public void ShouldCalculateGlobalGoalWithDecimalValues()
    {
        var result = _calculator.CalculateGlobalGoal(DecimalGoals);

        Assert.Equal(1250m, result);
    }

    [Fact]
    public void ShouldNotApplyMinimum1500RuleInsideGoalCalculator()
    {
        var result = _calculator.CalculateGlobalGoal(BelowMinimumGoals);

        Assert.Equal(900m, result);
    }

    [Fact]
    public void ShouldRejectNullStudyArea()
    {
        var studyPlan = StudyPlan.Create("Standard", 1m);

        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateIndividualGoal(null!, studyPlan));
    }

    [Fact]
    public void ShouldRejectNullStudyPlan()
    {
        var studyArea = StudyArea.Create("Mathematics", 600);

        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateIndividualGoal(studyArea, null!));
    }

    [Fact]
    public void ShouldRejectNullIndividualGoals()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateGlobalGoal(null!));
    }

    [Fact]
    public void ShouldRejectEmptyIndividualGoals()
    {
        Assert.Throws<ArgumentException>(() => _calculator.CalculateGlobalGoal(Array.Empty<decimal>()));
    }

    [Fact]
    public void ShouldRejectNonPositiveIndividualGoal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CalculateGlobalGoal(InvalidGoals));
    }

    [Fact]
    public void ShouldCalculateGlobalGoalForManyAreas()
    {
        var goals = Enumerable.Repeat(250m, 10).ToArray();

        var result = _calculator.CalculateGlobalGoal(goals);

        Assert.Equal(2500m, result);
    }

    [Fact]
    public void ShouldPreserveDecimalPrecision()
    {
        var studyArea = StudyArea.Create("Languages", 333);
        var studyPlan = StudyPlan.Create("Weighted", 1.25m);

        var result = _calculator.CalculateIndividualGoal(studyArea, studyPlan);

        Assert.Equal(416.25m, result);
    }
}