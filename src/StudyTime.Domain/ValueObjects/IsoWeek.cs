using System.Globalization;

using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct IsoWeek
{
    public int Year { get; }

    public int WeekNumber { get; }

    public IsoWeek(int year, int weekNumber)
    {
        if (year <= 0)
        {
            throw new DomainValidationException("ISO week year must be greater than zero.");
        }

        if (weekNumber is < 1 or > 53)
        {
            throw new DomainValidationException("ISO week number must be between 1 and 53.");
        }

        Year = year;
        WeekNumber = weekNumber;
    }

    public static IsoWeek FromDate(DateOnly date)
    {
        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);

        int year = ISOWeek.GetYear(dateTime);
        int weekNumber = ISOWeek.GetWeekOfYear(dateTime);

        return new IsoWeek(year, weekNumber);
    }

    public override string ToString()
    {
        return $"{Year}-W{WeekNumber:00}";
    }
}