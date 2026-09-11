using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct StudyMinutes
{
    public int Value { get; }

    private StudyMinutes(int value)
    {
        Value = value;
    }

    public static StudyMinutes From(int value)
    {
        if (value <= 0)
        {
            throw DomainRuleViolationException.R05_MinutesMustBePositive();
        }

        return new StudyMinutes(value);
    }

    public static implicit operator int(StudyMinutes minutes)
         => minutes.Value;

    public override string ToString() 
        => Value.ToString();

}