namespace StudyTime.Domain.ValueObjects;

public sealed record Minutes
{
    public int Value { get; }

    public Minutes(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Minutes must be greater than zero.");
        }

        Value = value;
    }

    public static implicit operator int(Minutes minutes)
    {
        ArgumentNullException.ThrowIfNull(minutes);
        return minutes.Value;
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}