namespace StudyTime.Domain.ValueObjects;

public sealed record Coefficient
{
    public decimal Value { get; }

    public Coefficient(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Coefficient must be greater than zero.");
        }

        Value = value;
    }

    public static implicit operator decimal(Coefficient coefficient)
    {
        ArgumentNullException.ThrowIfNull(coefficient);
        return coefficient.Value;
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}