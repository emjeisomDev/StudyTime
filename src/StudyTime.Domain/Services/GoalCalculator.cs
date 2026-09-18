using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Services;

public static class GoalCalculator
{
    public static decimal Calculate(decimal stdWeekStudyTime, Coefficient coefficient)
        => stdWeekStudyTime * coefficient.Value;
    
    public static decimal CalculateGlobal(IEnumerable<decimal> individualGoals)
        => individualGoals.Sum();
    
}