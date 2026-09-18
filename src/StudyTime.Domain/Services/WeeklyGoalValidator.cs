using StudyTime.Domain.Constants;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Services;

public static class WeeklyGoalValidator
{
    public static void Validate(decimal globalGoal)
    {
        if (globalGoal < BusinessConstants.MinimumWeeklyGoalMinutes)
        {
            throw new WeeklyGoalNotMetException(
                $"The global weekly goal must be at least {BusinessConstants.MinimumWeeklyGoalMinutes} minutes.");
        }
    }
}