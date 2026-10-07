using System.Globalization;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Infrastructure.Calendar;

public sealed class IsoWeekCalendar : IIsoWeekCalendar
{
    public IsoWeek GetIsoWeek(DateOnly weekStartDate)
    {
        DateTime date = weekStartDate.ToDateTime(TimeOnly.MinValue);

        int year = ISOWeek.GetYear(date);
        int weekNumber = ISOWeek.GetWeekOfYear(date);

        return new IsoWeek(year, weekNumber);
    }

    public DateOnly GetMonday(int year, int weekNumber)
    {
        DateTime monday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);
        
        return DateOnly.FromDateTime(monday);
    }
}