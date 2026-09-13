using System.Globalization;
using StudyTime.Domain.Exceptions;
using StudyTime.Application.Abstractions;

namespace StudyTime.Infrastructure.Time;

public sealed class SystemWeekCalendar : IWeekCalendar
{
    private readonly IApplicationClock _clock;

    public SystemWeekCalendar(IApplicationClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public DateOnly GetCurrentWeekStart()
        => GetMonday(_clock.Today);
    

    public DateOnly GetPreviousWeekStart()
        => GetCurrentWeekStart().AddDays(-7);

    public DateOnly GetNextWeekStart()
        => GetCurrentWeekStart().AddDays(7);
    
    public (int Year, int WeekNumber) GetIsoYearWeek(DateOnly date)
    {
        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);
        int year = ISOWeek.GetYear(dateTime);
        int weekNumber = ISOWeek.GetWeekOfYear(dateTime);
        return (year, weekNumber);
    }

    public bool IsMonday(DateOnly date)
        => date.DayOfWeek == DayOfWeek.Monday;
    

    public DateOnly GetSundayOf(DateOnly weekStart)
    {
        if (!IsMonday(weekStart))
        {
            throw DomainRuleViolationException.R07_WeekMustStartOnMonday(weekStart);
        }

        return weekStart.AddDays(6);
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}