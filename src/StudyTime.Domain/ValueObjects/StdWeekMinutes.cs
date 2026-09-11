using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct StdWeekMinutes
{
    public int Value { get; }

    private StdWeekMinutes(int value)
    {
        Value = value;
    }

    public static StdWeekMinutes From(int value)
    {
        if (value <= 0)
        {
            throw DomainRuleViolationException.R03_StdWeekStudyTimeMustBePositive();
        }

        return new StdWeekMinutes(value);
    }

    public static implicit operator int(StdWeekMinutes minutes)
        => minutes.Value;
    
    public override string ToString()
        => Value.ToString();
    
}