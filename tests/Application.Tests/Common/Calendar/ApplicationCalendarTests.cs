using StudyTime.Application.Common.Calendar;

namespace Application.Tests.Common.Calendar;

public sealed class ApplicationCalendarTests
{
    [Fact]
    public void ShouldDetermineMondayAsWeekStart()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 9, 2));

        var week = calendar.CurrentWeek;

        Assert.Equal(new DateOnly(2026, 8, 31), week.WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 6), week.WeekEndDate);
    }

    [Fact]
    public void ShouldDetermineCurrentPreviousAndNextWeeks()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        Assert.Equal(new DateOnly(2026, 8, 31), calendar.CurrentWeek.WeekStartDate);
        Assert.Equal(new DateOnly(2026, 8, 24), calendar.PreviousWeek.WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 7), calendar.NextWeek.WeekStartDate);
    }

    [Fact]
    public void ShouldReturnSameIsoWeekForEveryDateWithinTheWeek()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        var monday = calendar.GetWeek(new DateOnly(2026, 8, 31));
        var sunday = calendar.GetWeek(new DateOnly(2026, 9, 6));

        Assert.Equal(monday, sunday);
        Assert.Equal(new DateOnly(2026, 8, 31), monday.WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 6), monday.WeekEndDate);
    }

    [Fact]
    public void ShouldCalculateIsoYearAndWeekFromWeekStartDate()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        var week = calendar.GetWeek(new DateOnly(2026, 8, 31));

        Assert.Equal(2026, week.IsoYear);
        Assert.Equal(36, week.IsoWeek);
    }

    [Fact]
    public void ShouldHandleIsoYearBoundaryAtEndOfDecember()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 12, 31));

        var week = calendar.GetWeek(new DateOnly(2026, 12, 31));

        Assert.Equal(new DateOnly(2026, 12, 28), week.WeekStartDate);
        Assert.Equal(new DateOnly(2027, 1, 3), week.WeekEndDate);
        Assert.Equal(2026, week.IsoYear);
        Assert.Equal(53, week.IsoWeek);
    }

    [Fact]
    public void ShouldHandleIsoYearBoundaryAtBeginningOfJanuary()
    {
        var calendar = CreateCalendar(new DateOnly(2027, 1, 1));

        var week = calendar.GetWeek(new DateOnly(2027, 1, 1));

        Assert.Equal(new DateOnly(2026, 12, 28), week.WeekStartDate);
        Assert.Equal(new DateOnly(2027, 1, 3), week.WeekEndDate);
        Assert.Equal(2026, week.IsoYear);
        Assert.Equal(53, week.IsoWeek);
    }

    [Fact]
    public void ShouldUseClockTodayForCurrentWeek()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 9, 6));

        Assert.Equal(new DateOnly(2026, 8, 31), calendar.CurrentWeek.WeekStartDate);
    }

    [Fact]
    public void ShouldRejectCalendarWithNullClock()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplicationCalendar(null!));
    }

    [Fact]
    public void ShouldRejectNonMondayWeekStart()
    {
        Assert.Throws<ArgumentException>(() => new ApplicationWeek(new DateOnly(2026, 9, 2)));
    }

    private static ApplicationCalendar CreateCalendar(DateOnly today)
        => new(new FixedApplicationClock(today));

    private sealed class FixedApplicationClock : StudyTime.Application.Common.Clock.IApplicationClock
    {
        public FixedApplicationClock(DateOnly today)
        {
            Today = today;
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            Now = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeZone.BaseUtcOffset);
        }

        public DateTimeOffset Now { get; }
        public DateOnly Today { get; }
        public TimeZoneInfo TimeZone { get; }
    }
}