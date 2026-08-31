namespace StudyTime.Application.Common.Clock;

/// <summary>
/// Provides the current application time using the official application timezone.
/// </summary>
public interface IApplicationClock
{
    /// <summary>
    /// Gets the current date and time converted to the official application timezone.
    /// </summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Gets the current calendar date in the official application timezone.
    /// </summary>
    DateOnly Today { get; }

    /// <summary>
    /// Gets the timezone used by the application clock.
    /// </summary>
    TimeZoneInfo TimeZone { get; }
}