namespace StudyTime.Application.Common.Calendar;

/// <summary>
/// Provides application calendar operations based on the official application clock.
/// </summary>
public interface IApplicationCalendar
{
    ApplicationWeek CurrentWeek { get; }
    ApplicationWeek PreviousWeek { get; }
    ApplicationWeek NextWeek { get; }
    ApplicationWeek GetWeek(DateOnly dateWeek);
}