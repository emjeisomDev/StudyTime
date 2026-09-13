using StudyTime.Application.Abstractions;

namespace StudyTime.Infrastructure.Time;

public sealed class SystemApplicationClock : IApplicationClock
{
    private const string OfficialTimeZoneId = "America/Sao_Paulo";
    private readonly TimeZoneInfo _timeZone;

    public SystemApplicationClock()
        => _timeZone = TimeZoneInfo.FindSystemTimeZoneById(OfficialTimeZoneId);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(UtcNow, _timeZone);
    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
    public TimeZoneInfo TimeZone => _timeZone;
}