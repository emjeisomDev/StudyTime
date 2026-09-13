using StudyTime.Application.Abstractions;
using StudyTime.Infrastructure.Time;
using Xunit;

namespace Application.Tests.Time;

public sealed class SystemWeekCalendarTests
{
    [Fact]
    [Trait("Category", "CrossCutting")]
    public void GetCurrentWeekStart_WhenTodayIsWednesday_ReturnsMonday()
    {
        DateOnly today = new(2026, 9, 2);

        FixedApplicationClock clock = new(today);
        SystemWeekCalendar calendar = new(clock);

        Assert.Equal(
            new DateOnly(2026, 8, 31),
            calendar.GetCurrentWeekStart());
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void GetPreviousWeekStart_WhenCurrentWeekExists_ReturnsSevenDaysBefore()
    {
        FixedApplicationClock clock = new(new DateOnly(2026, 9, 2));
        SystemWeekCalendar calendar = new(clock);

        Assert.Equal(
            new DateOnly(2026, 8, 24),
            calendar.GetPreviousWeekStart());
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void GetNextWeekStart_WhenCurrentWeekExists_ReturnsSevenDaysAfter()
    {
        FixedApplicationClock clock = new(new DateOnly(2026, 9, 2));
        SystemWeekCalendar calendar = new(clock);

        Assert.Equal(
            new DateOnly(2026, 9, 7),
            calendar.GetNextWeekStart());
    }

    [Theory]
    [Trait("Category", "CrossCutting")]
    [InlineData(2026, 12, 28, 2026, 53)]
    [InlineData(2027, 1, 3, 2026, 53)]
    [InlineData(2025, 12, 29, 2026, 1)]
    [InlineData(2027, 1, 4, 2027, 1)]
    public void GetIsoYearWeek_WhenDateIsAtIsoYearBoundary_ReturnsExpectedIsoWeek(
        int year,
        int month,
        int day,
        int expectedYear,
        int expectedWeek)
    {
        FixedApplicationClock clock = new(new DateOnly(2026, 1, 1));
        SystemWeekCalendar calendar = new(clock);

        (int actualYear, int actualWeek) =
            calendar.GetIsoYearWeek(new DateOnly(year, month, day));

        Assert.Equal(expectedYear, actualYear);
        Assert.Equal(expectedWeek, actualWeek);
    }

    [Theory]
    [Trait("Category", "CrossCutting")]
    [InlineData(2026, 8, 31, true)]
    [InlineData(2026, 9, 1, false)]
    [InlineData(2026, 9, 2, false)]
    [InlineData(2026, 9, 3, false)]
    [InlineData(2026, 9, 4, false)]
    [InlineData(2026, 9, 5, false)]
    [InlineData(2026, 9, 6, false)]
    public void IsMonday_WhenDateIsEvaluated_ReturnsExpectedResult(
        int year,
        int month,
        int day,
        bool expected)
    {
        FixedApplicationClock clock = new(new DateOnly(2026, 1, 1));
        SystemWeekCalendar calendar = new(clock);

        bool actual = calendar.IsMonday(new DateOnly(year, month, day));

        Assert.Equal(expected, actual);
    }

    [Fact]
    [Trait("Category", "CrossCutting")]
    public void GetSundayOf_WhenWeekStartsOnMonday_ReturnsSixDaysAfter()
    {
        FixedApplicationClock clock = new(new DateOnly(2026, 1, 1));
        SystemWeekCalendar calendar = new(clock);

        Assert.Equal(
            new DateOnly(2026, 9, 6),
            calendar.GetSundayOf(new DateOnly(2026, 8, 31)));
    }

    [Theory]
    [InlineData(2026, 12, 27, 2026, 52)]
    [InlineData(2026, 12, 28, 2026, 53)]
    [InlineData(2027, 1, 3, 2026, 53)]
    [InlineData(2027, 1, 4, 2027, 1)]
    public void GetIsoYearWeek_WhenCrossingIsoYearBoundary_ReturnsExpectedIsoWeek(
        int year,
        int month,
        int day,
        int expectedYear,
        int expectedWeek)
    {
        FixedApplicationClock clock = new(new DateOnly(2026, 1, 1));
        SystemWeekCalendar calendar = new(clock);

        (int actualYear, int actualWeek) =
            calendar.GetIsoYearWeek(new DateOnly(year, month, day));

        Assert.Equal(expectedYear, actualYear);
        Assert.Equal(expectedWeek, actualWeek);
    }

    private sealed class FixedApplicationClock : IApplicationClock
    {
        private readonly DateOnly _today;

        public FixedApplicationClock(DateOnly today)
        {
            _today = today;
        }

        public DateTimeOffset UtcNow =>
            new(_today.Year, _today.Month, _today.Day,
                12,
                0,
                0,
                TimeSpan.Zero);

        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => _today;
        public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    }
}