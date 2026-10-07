using StudyTime.Infrastructure.Calendar;

namespace StudyTime.Infrastructure.Tests.Time;

public sealed class IsoWeekCalendarTests
{
    [Fact]
    public void GetIsoWeek_MondayInOctober_ReturnsExpectedIsoWeek()
    {
        var calendar = new IsoWeekCalendar();
        var result = calendar.GetIsoWeek(new DateOnly(2026, 10, 5));

        Assert.Equal(2026, result.Year);
        Assert.Equal(41, result.WeekNumber);
    }

    [Fact]
    public void GetIsoWeek_MondayAtYearBoundary_ReturnsIsoYearAndWeek()
    {
        var calendar = new IsoWeekCalendar();
        var result = calendar.GetIsoWeek(new DateOnly(2027, 1, 4));

        Assert.Equal(2027, result.Year);
        Assert.Equal(1, result.WeekNumber);
    }

    [Fact]
    public void GetMonday_ValidIsoWeek_ReturnsMonday()
    {
        var calendar = new IsoWeekCalendar();
        var result = calendar.GetMonday(2026, 41);

        Assert.Equal(new DateOnly(2026, 10, 5), result);
    }

    [Fact]
    public void GetMonday_FirstIsoWeekOfYear_ReturnsCorrectDate()
    {
        var calendar = new IsoWeekCalendar();
        var result = calendar.GetMonday(2027, 1);
        Assert.Equal(new DateOnly(2027, 1, 4), result);
    }

    [Fact]
    public void GetIsoWeek_AndGetMonday_ReturnOriginalMonday()
    {
        var calendar = new IsoWeekCalendar();
        var expected = new DateOnly(2026, 10, 5);
        var isoWeek = calendar.GetIsoWeek(expected);
        var result = calendar.GetMonday(isoWeek.Year, isoWeek.WeekNumber);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetIsoWeek_LastMondayOfIsoYear_ReturnsWeek53WhenApplicable()
    {
        var calendar = new IsoWeekCalendar();
        var result = calendar.GetIsoWeek(new DateOnly(2026, 12, 28));

        Assert.Equal(2026, result.Year);
        Assert.Equal(53, result.WeekNumber);
    }
}