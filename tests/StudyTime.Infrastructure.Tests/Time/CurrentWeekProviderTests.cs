using StudyTime.Domain.Abstractions;
using StudyTime.Infrastructure.Time;

namespace StudyTime.Infrastructure.Tests.Time;

public sealed class CurrentWeekProviderTests
{
    [Fact]
    public void GetCurrentWeek_AtMondayMidnight_ReturnsCurrentMondayToSunday()
    {
        var clock = new FakeClock(new DateTime(2026, 10, 5, 3, 0, 0, DateTimeKind.Utc));
        ICurrentWeekProvider provider = new CurrentWeekProvider(clock);

        var result = provider.GetCurrentWeek();
        Assert.Equal(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Unspecified), result.Start);
        Assert.Equal(new DateTime(2026, 10, 11, 23, 59, 59, DateTimeKind.Unspecified), result.End);
    }

    [Fact]
    public void GetCurrentWeek_AtSundayEndOfDay_ReturnsSameCurrentWeek()
    {
        var clock = new FakeClock(new DateTime(2026, 10, 12, 2, 59, 59, DateTimeKind.Utc));
        ICurrentWeekProvider provider = new CurrentWeekProvider(clock);

        var result = provider.GetCurrentWeek();
        Assert.Equal(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Unspecified), result.Start);
        Assert.Equal(new DateTime(2026, 10, 11, 23, 59, 59, DateTimeKind.Unspecified), result.End);
    }

    [Fact]
    public void GetCurrentWeek_AtNextMondayMidnight_StartsNewWeek()
    {
        var clock = new FakeClock(new DateTime(2026, 10, 12, 3, 0, 0, DateTimeKind.Utc));
        ICurrentWeekProvider provider = new CurrentWeekProvider(clock);

        var result = provider.GetCurrentWeek();
        Assert.Equal(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Unspecified), result.Start);
        Assert.Equal(new DateTime(2026, 10, 18, 23, 59, 59, DateTimeKind.Unspecified), result.End);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("FakeClock requires a UTC DateTime.", nameof(utcNow));
            }

            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}