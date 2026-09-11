using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Services;

public interface IGoalCalculator
{
    public decimal CalculateIndividualGoal
        (StdWeekMinutes std, PlanCoefficient coeff);

    public decimal SumGlobalGoal
        (IEnumerable<StudyAreaWeek> weeks, 
        Func<StudyAreaWeek, StdWeekMinutes> resolveStd, 
        Func<StudyAreaWeek, PlanCoefficient> resolveCoeff);
}
