using System.Globalization;

using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct Coefficient
{
    public decimal Value { get; }

    public Coefficient(decimal value)
    {
        if (value <= 0)
        {
            throw new DomainValidationException("Coefficient must be greater than zero.");
        }

        Value = value;
    }

    public static decimal operator *(Coefficient coefficient, Minutes minutes)
    {
        return coefficient.Value * minutes.Value;
    }

    public static decimal operator *(Minutes minutes, Coefficient coefficient)
    {
        return coefficient.Value * minutes.Value;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}