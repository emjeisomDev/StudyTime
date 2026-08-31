using StudyTime.Application.Common.Clock;

namespace StudyTime.Infrastructure.Common.Clock;

/// <summary>
/// Provides application time converted to the timezone selected by the API.
/// </summary>
public sealed class ApplicationClock : IApplicationClock
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationClock"/> class.
    /// </summary>
    /// <param name="timeProvider">Provider used to obtain the current instant.</param>
    /// <param name="timeZone">Timezone selected by the application composition root.</param>
    public ApplicationClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(timeZone);
        _timeProvider = timeProvider;
        TimeZone = timeZone;
    }

    /// <inheritdoc />
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), TimeZone);

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    /// <inheritdoc />
    public TimeZoneInfo TimeZone { get; }
}