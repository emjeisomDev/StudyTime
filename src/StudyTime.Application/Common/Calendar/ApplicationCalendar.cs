using StudyTime.Application.Common.Clock;

namespace StudyTime.Application.Common.Calendar;

/// <summary>
/// Provides ISO calendar calculations using the application clock.
/// </summary>
public sealed class ApplicationCalendar : IApplicationCalendar
{
    private const int MaximumFutureWeeks = 4;
    private readonly IApplicationClock _clock;

    public ApplicationCalendar(IApplicationClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public ApplicationWeek CurrentWeek => GetWeek(_clock.Today);

    public ApplicationWeek PreviousWeek => CurrentWeek.AddWeeks(-1);

    public ApplicationWeek NextWeek => CurrentWeek.AddWeeks(1);

    public IReadOnlyList<ApplicationWeek> ConfigurationWeeks
        => Enumerable.Range(0, MaximumFutureWeeks + 1)
            .Select(CurrentWeek.AddWeeks)
            .ToArray();

    public ApplicationWeek GetWeek(DateOnly dateWeek)
    {
        var daysSinceMonday = ((int)dateWeek.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return new ApplicationWeek(dateWeek.AddDays(-daysSinceMonday));
    }

    public bool IsWithinConfigurationWindow(DateOnly weekStartDate)
    {
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            return false;

        var currentWeekStart = CurrentWeek.WeekStartDate;
        var weeksFromCurrent = (weekStartDate.DayNumber - currentWeekStart.DayNumber) / 7;

        return weeksFromCurrent >= 0 && weeksFromCurrent <= MaximumFutureWeeks;
    }
}