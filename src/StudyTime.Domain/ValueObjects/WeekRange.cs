namespace StudyTime.Domain.ValueObjects;

public sealed record WeekRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public WeekRange(DateTime start)
    {
        if (start.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException(
                "The week must start on a Monday.",
                nameof(start));
        }

        if (start.TimeOfDay != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The week must start at 00:00:00.",
                nameof(start));
        }

        Start = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        End = Start.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public WeekRange(DateOnly startDate)
        : this(startDate.ToDateTime(TimeOnly.MinValue))
    {
    }

    public bool Contains(DateTime value)
    {
        DateTime localValue = DateTime.SpecifyKind(
            value,
            DateTimeKind.Unspecified);

        return localValue >= Start && localValue <= End;
    }

    public override string ToString() =>
        $"{Start:yyyy-MM-dd HH:mm:ss} - {End:yyyy-MM-dd HH:mm:ss}";
}