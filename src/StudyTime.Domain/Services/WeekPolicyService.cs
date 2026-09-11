using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Services;

public sealed class WeekPolicyService : IWeekPolicyService
{
    public const decimal MinimumGlobalGoal = 1500m;

    public bool IsWeekValidForRecords(IEnumerable<decimal> individualGoals)
    {
        ArgumentNullException.ThrowIfNull(individualGoals);

        var goals = individualGoals.ToList();
        return goals.Sum() >= MinimumGlobalGoal;
    }

    public void EnsureWeekHasMinimumGoal(IEnumerable<decimal> individualGoals)
    {
        ArgumentNullException.ThrowIfNull(individualGoals);

        var sum = individualGoals.Sum();
        if (sum < MinimumGlobalGoal)
        {
            throw DomainRuleViolationException.R11_GlobalGoalBelowMinimum(sum);
        }
    }

    public bool IsWeekStartOnMonday(DateOnly weekStart)
    {
        WeekStartDate.From(weekStart);
        return true;
    }

    public DateOnly GetSundayOf(DateOnly weekStart)
    {
        WeekStartDate.From(weekStart);
        return weekStart.AddDays(6);
    }

    public bool IsDateWithinWeek(DateOnly date, DateOnly weekStart)
    {
        WeekStartDate.From(weekStart);

        var sunday = weekStart.AddDays(6);
        return date >= weekStart && date <= sunday;
    }
}