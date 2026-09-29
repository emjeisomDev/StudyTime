using System.Globalization;

namespace StudyTime.Domain.ValueObjects;

public sealed record IsoWeek
{
    public int Year { get; }
    public int WeekNumber { get; }

    public IsoWeek(int year, int weekNumber)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                year,
                "Year must be greater than zero.");
        }

        if (weekNumber is < 1 or > 53)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weekNumber),
                weekNumber,
                "ISO week number must be between 1 and 53.");
        }

        int weeksInYear = ISOWeek.GetWeeksInYear(year);

        if (weekNumber > weeksInYear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weekNumber),
                weekNumber,
                $"ISO year {year} has only {weeksInYear} weeks.");
        }

        Year = year;
        WeekNumber = weekNumber;
    }

    public DateOnly GetMonday()
    {
        DateTime monday = ISOWeek.ToDateTime(
            Year,
            WeekNumber,
            DayOfWeek.Monday);

        return DateOnly.FromDateTime(monday);
    }

    public override string ToString() => $"{Year}-W{WeekNumber:D2}";
}