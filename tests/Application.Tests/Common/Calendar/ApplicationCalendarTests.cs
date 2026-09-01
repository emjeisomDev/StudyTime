using StudyTime.Application.Common.Calendar;
using StudyTime.Application.Common.Clock;

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
    public void ShouldExposeCurrentWeekAndFourFollowingWeeks()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        var weeks = calendar.ConfigurationWeeks;

        Assert.Equal(5, weeks.Count);
        Assert.Equal(new DateOnly(2026, 8, 31), weeks[0].WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 7), weeks[1].WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 14), weeks[2].WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 21), weeks[3].WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 28), weeks[4].WeekStartDate);
    }

    [Fact]
    public void ShouldAllowCurrentWeek()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        Assert.True(calendar.IsWithinConfigurationWindow(new DateOnly(2026, 8, 31)));
    }

    [Fact]
    public void ShouldAllowFourFollowingWeeks()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        Assert.True(calendar.IsWithinConfigurationWindow(new DateOnly(2026, 9, 28)));
    }

    [Fact]
    public void ShouldRejectFifthFollowingWeek()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        Assert.False(calendar.IsWithinConfigurationWindow(new DateOnly(2026, 10, 5)));
    }

    [Fact]
    public void ShouldRejectPreviousWeek()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        Assert.False(calendar.IsWithinConfigurationWindow(new DateOnly(2026, 8, 24)));
    }

    [Fact]
    public void ShouldRejectNonMondayConfigurationDate()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        Assert.False(calendar.IsWithinConfigurationWindow(new DateOnly(2026, 9, 2)));
    }

    [Fact]
    public void ShouldCalculateWeekByDateRegardlessOfDayOfWeek()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        var week = calendar.GetWeek(new DateOnly(2026, 9, 6));

        Assert.Equal(new DateOnly(2026, 8, 31), week.WeekStartDate);
        Assert.Equal(new DateOnly(2026, 9, 6), week.WeekEndDate);
    }

    [Fact]
    public void ShouldAddWeeksWithoutChangingWeekday()
    {
        var calendar = CreateCalendar(new DateOnly(2026, 8, 31));

        var week = calendar.CurrentWeek.AddWeeks(4);

        Assert.Equal(new DateOnly(2026, 9, 28), week.WeekStartDate);
        Assert.Equal(DayOfWeek.Monday, week.WeekStartDate.DayOfWeek);
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

    private sealed class FixedApplicationClock : IApplicationClock
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