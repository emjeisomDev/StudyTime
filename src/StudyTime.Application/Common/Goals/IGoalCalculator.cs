using StudyTime.Domain.Entities;

namespace StudyTime.Application.Common.Goals;

public interface IGoalCalculator
{
    decimal CalculateIndividualGoal(StudyArea studyArea, StudyPlan studyPlan);
    decimal CalculateGlobalGoal(IEnumerable<decimal> individualGoals);
}