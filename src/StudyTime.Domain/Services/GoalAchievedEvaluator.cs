namespace StudyTime.Domain.Services;

public static class GoalAchievedEvaluator
{
    public static bool IndividualAchieved(
        int minutesStudied,
        decimal weekIndividualGoal)
    {
        return minutesStudied >= weekIndividualGoal;
    }

    public static bool GlobalAchieved(IEnumerable<(int minutes, decimal goal)> entries)
    {
        bool hasEntries = false;

        foreach ((int minutes, decimal goal) in entries)
        {
            hasEntries = true;

            if (!IndividualAchieved(minutes, goal))
            {
                return false;
            }
        }

        return hasEntries;
    }
}