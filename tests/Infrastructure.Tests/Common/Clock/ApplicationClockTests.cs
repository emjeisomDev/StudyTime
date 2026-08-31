using StudyTime.Infrastructure.Common.Clock;

namespace Infrastructure.Tests.Common.Clock;

public sealed class ApplicationClockTests
{
    private static readonly TimeZoneInfo SaoPauloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [Fact]
    public void ShouldUseConfiguredTimezone()
    {
        var clock = new ApplicationClock(TimeProvider.System, SaoPauloTimeZone);

        Assert.Equal("America/Sao_Paulo", clock.TimeZone.Id);
    }

    [Fact]
    public void ShouldConvertUtcInstantToSaoPauloTime()
    {
        var utcInstant = new DateTimeOffset(2026, 8, 31, 2, 30, 0, TimeSpan.Zero);
        var clock = new ApplicationClock(new FixedTimeProvider(utcInstant), SaoPauloTimeZone);

        Assert.Equal(2026, clock.Now.Year);
        Assert.Equal(8, clock.Now.Month);
        Assert.Equal(30, clock.Now.Day);
        Assert.Equal(23, clock.Now.Hour);
        Assert.Equal(-3, clock.Now.Offset.Hours);
    }

    [Fact]
    public void TodayShouldMatchDateFromConvertedNow()
    {
        var utcInstant = new DateTimeOffset(2026, 8, 31, 2, 30, 0, TimeSpan.Zero);
        var clock = new ApplicationClock(new FixedTimeProvider(utcInstant), SaoPauloTimeZone);

        Assert.Equal(new DateOnly(2026, 8, 30), clock.Today);
        Assert.Equal(DateOnly.FromDateTime(clock.Now.DateTime), clock.Today);
    }

    [Fact]
    public void ShouldReturnDeterministicValueFromTimeProvider()
    {
        var utcInstant = new DateTimeOffset(2026, 12, 31, 2, 15, 30, TimeSpan.Zero);
        var clock = new ApplicationClock(new FixedTimeProvider(utcInstant), SaoPauloTimeZone);

        Assert.Equal(new DateTimeOffset(2026, 12, 30, 23, 15, 30, TimeSpan.FromHours(-3)), clock.Now);
    }

    [Fact]
    public void ShouldRejectNullTimeProvider()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplicationClock(null!, SaoPauloTimeZone));
    }

    [Fact]
    public void ShouldRejectNullTimezone()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplicationClock(TimeProvider.System, null!));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}