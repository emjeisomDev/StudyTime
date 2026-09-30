using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Services;

public static class WeeklyGoalValidator
{
    public const decimal MinimumWeeklyGoal = 1500m;

    public static void Validate(decimal globalGoal)
    {
        if (globalGoal < MinimumWeeklyGoal)
        {
            throw new WeeklyGoalNotMetException(
                $"The weekly global goal must be at least " +
                $"{MinimumWeeklyGoal} minutes. Actual: {globalGoal}.");
        }
    }

    public static void Validate(
        IEnumerable<decimal> individualGoals)
    {
        ArgumentNullException.ThrowIfNull(individualGoals);

        var globalGoal = GoalCalculator.CalculateGlobalGoal(
            individualGoals);

        Validate(globalGoal);
    }
}