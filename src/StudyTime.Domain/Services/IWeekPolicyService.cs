namespace StudyTime.Domain.Services;

public interface IWeekPolicyService
{
    public bool IsWeekValidForRecords(IEnumerable<decimal> individualGoals);
    public void EnsureWeekHasMinimumGoal(IEnumerable<decimal> individualGoals);
    public bool IsWeekStartOnMonday(DateOnly weekStart);
    public DateOnly GetSundayOf(DateOnly weekStart);
    public bool IsDateWithinWeek(DateOnly date, DateOnly weekStart);
}