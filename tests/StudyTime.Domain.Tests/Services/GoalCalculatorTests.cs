using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class GoalCalculatorTests
{
    [Theory]
    [InlineData(1000, 1.5, 1500)]
    [InlineData(1200, 1.25, 1500)]
    [InlineData(800, 2, 1600)]
    public void CalculateIndividualGoal_ValidValues_ReturnsProduct(
        int standardWeeklyStudyTime,
        decimal coefficient,
        decimal expected)
    {
        decimal result = GoalCalculator.CalculateIndividualGoal(
            standardWeeklyStudyTime,
            coefficient);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateIndividualGoal_NonPositiveStandardTime_Throws(
        int standardWeeklyStudyTime)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GoalCalculator.CalculateIndividualGoal(
                standardWeeklyStudyTime,
                1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateIndividualGoal_NonPositiveCoefficient_Throws(
        decimal coefficient)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GoalCalculator.CalculateIndividualGoal(
                1000,
                coefficient));
    }

    [Fact]
    public void CalculateGlobalGoal_MultipleGoals_ReturnsSum()
    {
        decimal result = GoalCalculator.CalculateGlobalGoal(
            new[] { 500m, 600m, 400m });

        Assert.Equal(1500m, result);
    }

    [Fact]
    public void CalculateGlobalGoal_EmptyCollection_ReturnsZero()
    {
        decimal result = GoalCalculator.CalculateGlobalGoal(
            Array.Empty<decimal>());

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateGlobalGoal_NullCollection_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => GoalCalculator.CalculateGlobalGoal(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateGlobalGoal_NonPositiveIndividualGoal_Throws(
        decimal individualGoal)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GoalCalculator.CalculateGlobalGoal(
                new[] { 500m, individualGoal }));
    }
}