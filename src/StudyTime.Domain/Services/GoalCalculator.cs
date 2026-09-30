namespace StudyTime.Domain.Services;

public static class GoalCalculator
{
    public static decimal CalculateIndividualGoal(
        int standardWeeklyStudyTime,
        decimal coefficient)
    {
        if (standardWeeklyStudyTime <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(standardWeeklyStudyTime),
                "Standard weekly study time must be positive.");
        }

        if (coefficient <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coefficient),
                "Coefficient must be positive.");
        }

        return standardWeeklyStudyTime * coefficient;
    }

    public static decimal CalculateGlobalGoal(
        IEnumerable<decimal> individualGoals)
    {
        ArgumentNullException.ThrowIfNull(individualGoals);

        decimal total = 0;

        foreach (var goal in individualGoals)
        {
            if (goal <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(individualGoals),
                    "Individual goals must be positive.");
            }

            total += goal;
        }

        return total;
    }
}