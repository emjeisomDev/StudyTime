using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct WeekStartDate
{
    public DateOnly Value { get; }

    private WeekStartDate(DateOnly value)
    {
        Value = value;
    }

    public static WeekStartDate From(DateOnly date)
    {
        if (date.DayOfWeek != DayOfWeek.Monday)
        {
            throw DomainRuleViolationException.R07_WeekMustStartOnMonday(date);
        }

        return new WeekStartDate(date);
    }

    public static implicit operator DateOnly(WeekStartDate weekStartDate)
    {
        return weekStartDate.Value;
    }

    public override string ToString()
    {
        return Value.ToString("yyyy-MM-dd");
    }
}