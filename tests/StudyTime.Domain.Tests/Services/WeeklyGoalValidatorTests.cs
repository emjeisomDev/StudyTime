using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class WeeklyGoalValidatorTests
{
    [Fact]
    public void Validate_WhenGoalIs1500_DoesNotThrow()
    {
        WeeklyGoalValidator.Validate(1500m);
    }

    [Fact]
    public void Validate_WhenGoalIsGreaterThan1500_DoesNotThrow()
    {
        WeeklyGoalValidator.Validate(1800m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1499)]
    [InlineData(1499.99)]
    public void Validate_WhenGoalIsBelowMinimum_ThrowsWeeklyGoalNotMetException(decimal goal)
    {
        Assert.Throws<WeeklyGoalNotMetException>(
            () => WeeklyGoalValidator.Validate(goal));
    }

    [Fact]
    public void Validate_WhenIndividualGoalsMeetMinimum_DoesNotThrow()
    {
        decimal[] goals = [600m, 500m, 400m];

        WeeklyGoalValidator.Validate(goals);
    }

    [Fact]
    public void Validate_WhenIndividualGoalsAreBelowMinimum_ThrowsWeeklyGoalNotMetException()
    {
        decimal[] goals = [500m, 500m, 499m];

        Assert.Throws<WeeklyGoalNotMetException>(
            () => WeeklyGoalValidator.Validate(goals));
    }

    [Fact]
    public void Validate_WhenIndividualGoalsAreEmpty_ThrowsWeeklyGoalNotMetException()
    {
        decimal[] goals = [];

        Assert.Throws<WeeklyGoalNotMetException>(
            () => WeeklyGoalValidator.Validate(goals));
    }

    [Fact]
    public void Validate_WhenIndividualGoalsAreNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => WeeklyGoalValidator.Validate(
                (IEnumerable<decimal>)null!));
    }
}