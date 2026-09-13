using StudyTime.Application.Abstractions;

namespace StudyTime.Infrastructure.Time;

public sealed class SystemApplicationClock : IApplicationClock
{
    private const string OfficialTimeZoneId = "America/Sao_Paulo";
    private readonly TimeZoneInfo _timeZone;
    private readonly Func<DateTimeOffset> _utcNowProvider;

    public SystemApplicationClock() : this(() => DateTimeOffset.UtcNow)
    { }

    public SystemApplicationClock(Func<DateTimeOffset> utcNowProvider)
    {
        ArgumentNullException.ThrowIfNull(utcNowProvider);
        _utcNowProvider = utcNowProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(OfficialTimeZoneId);
    }

    public DateTimeOffset UtcNow => _utcNowProvider();
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(UtcNow, _timeZone);
    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
    public TimeZoneInfo TimeZone => _timeZone;
}