using StudyTime.Domain.Entities;

namespace StudyTime.Application.Common.Goals;

public sealed class GoalCalculator : IGoalCalculator
{
    public decimal CalculateIndividualGoal(StudyArea studyArea, StudyPlan studyPlan)
    {
        ArgumentNullException.ThrowIfNull(studyArea);
        ArgumentNullException.ThrowIfNull(studyPlan);
        return studyArea.StdWeekStudyTime * studyPlan.Coefficient;
    }

    public decimal CalculateGlobalGoal(IEnumerable<decimal> individualGoals)
    {
        ArgumentNullException.ThrowIfNull(individualGoals);

        var goals = individualGoals.ToArray();
        if (goals.Length == 0)
            throw new ArgumentException("At least one individual goal is required.", nameof(individualGoals));
        if (goals.Any(goal => goal <= 0))
            throw new ArgumentOutOfRangeException(nameof(individualGoals), "Individual goals must be greater than zero.");

        return goals.Sum();
    }
}