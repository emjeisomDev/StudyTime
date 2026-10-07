using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Infrastructure.Time;

public sealed class CurrentWeekProvider : ICurrentWeekProvider
{
    private const string OfficialTimeZoneId = "America/Sao_Paulo";

    private readonly IClock _clock;
    private readonly TimeZoneInfo _officialTimeZone;

    public CurrentWeekProvider(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
        _officialTimeZone = TimeZoneInfo.FindSystemTimeZoneById(OfficialTimeZoneId);
    }

    public WeekRange GetCurrentWeek()
    {
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc),
            _officialTimeZone);

        int daysSinceMonday = ((int)localNow.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

        DateTime monday = localNow.Date.AddDays(-daysSinceMonday);

        return new WeekRange(monday);
    }
}