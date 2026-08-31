using StudyTime.Application.Common.Clock;

namespace StudyTime.Application.Common.Calendar;

/// <summary>
/// Provides ISO calendar calculations using the application clock.
/// </summary>
public sealed class ApplicationCalendar : IApplicationCalendar
{
    private readonly IApplicationClock _clock;

    public ApplicationCalendar(IApplicationClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public ApplicationWeek CurrentWeek => GetWeek(_clock.Today);

    public ApplicationWeek PreviousWeek => GetWeek(_clock.Today.AddDays(-7));

    public ApplicationWeek NextWeek => GetWeek(_clock.Today.AddDays(7));

    public ApplicationWeek GetWeek(DateOnly dateWeek)
    {
        var daysSinceMonday = ((int)dateWeek.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStartDate = dateWeek.AddDays(-daysSinceMonday);
        return new ApplicationWeek(weekStartDate);
    }
}