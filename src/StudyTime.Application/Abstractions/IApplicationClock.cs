namespace StudyTime.Application.Abstractions;

public interface IApplicationClock
{
    public DateTimeOffset UtcNow { get; }
    public DateTimeOffset Now { get; }
    public DateOnly Today { get; }
    public TimeZoneInfo TimeZone { get; }
}
