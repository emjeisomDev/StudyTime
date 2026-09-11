using System.Globalization;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct IsoYearWeek
{
    public int Year { get; }

    public int WeekNumber { get; }

    private IsoYearWeek(int year, int weekNumber)
    {
        Year = year;
        WeekNumber = weekNumber;
    }

    public static IsoYearWeek From(int year, int weekNumber)
    {
        if (year <= 0 || weekNumber is < 1 or > 53)
        {
            throw DomainRuleViolationException.R21_InvalidIsoWeek(year, weekNumber);
        }

        return new IsoYearWeek(year, weekNumber);
    }

    public static IsoYearWeek From(DateOnly date)
    {
        var year = ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue));
        var week = ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));

        return From(year, week);
    }

    public DateOnly GetMonday()
        => DateOnly.FromDateTime(ISOWeek.ToDateTime(Year, WeekNumber, DayOfWeek.Monday));
    

    public override string ToString()
        => $"{Year}-W{WeekNumber:00}";
    
}