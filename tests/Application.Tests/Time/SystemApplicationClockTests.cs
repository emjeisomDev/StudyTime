using Xunit;
using StudyTime.Infrastructure.Time;

namespace Application.Tests.Time;

public sealed class SystemApplicationClockTests
{
    [Fact]
    [Trait("Category", "CrossCutting")]
    public void Today_WhenUtcProcessTimezoneIsUsed_ReturnsOfficialSaoPauloDate()
    {
        DateTimeOffset utcInstant = new(2026, 8, 31, 3, 0, 0, TimeSpan.Zero);
        SystemApplicationClock clock = new(() => utcInstant);
        Assert.Equal(new DateOnly(2026, 8, 31), clock.Today);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void Now_WhenUtcInstantIsBeforeSaoPauloMidnight_ReturnsPreviousLocalDate()
    {
        DateTimeOffset utcInstant = new(2026, 8, 31, 2, 59, 0, TimeSpan.Zero);
        SystemApplicationClock clock = new(() => utcInstant);
        Assert.Equal(new DateTimeOffset(2026, 8, 30, 23, 59, 0, TimeSpan.FromHours(-3)), clock.Now);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void Now_WhenUtcInstantIsExactlySaoPauloMidnight_ReturnsCurrentLocalDate()
    {
        DateTimeOffset utcInstant = new(2026, 8, 31, 3, 0, 0, TimeSpan.Zero);
        SystemApplicationClock clock = new(() => utcInstant);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.FromHours(-3)), clock.Now);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void UtcNow_WhenProviderReturnsFixedInstant_ReturnsSameInstant()
    {
        DateTimeOffset expected = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        SystemApplicationClock clock = new(() => expected);
        Assert.Equal(expected, clock.UtcNow);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void TimeZone_ReturnsAmericaSaoPaulo()
    {
        SystemApplicationClock clock = new(() => DateTimeOffset.UtcNow);
        Assert.Equal("America/Sao_Paulo", clock.TimeZone.Id);
    }
}