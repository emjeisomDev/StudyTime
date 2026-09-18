using System.Globalization;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct Minutes
{
    public int Value { get; }

    public Minutes(int value)
    {
        if (value <= 0)
        {
            throw new DomainException("Minutes must be greater than zero.");
        }

        Value = value;
    }

    public static implicit operator int(Minutes minutes)
    {
        return minutes.Value;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}