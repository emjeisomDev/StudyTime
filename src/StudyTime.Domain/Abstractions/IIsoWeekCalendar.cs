using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Abstractions;

/// <summary>
/// Provides ISO-8601 week calculations.
/// </summary>
public interface IIsoWeekCalendar
{
    public IsoWeek GetIsoWeek(DateOnly weekStartDate);

    public DateOnly GetMonday(int year, int weekNumber);
}