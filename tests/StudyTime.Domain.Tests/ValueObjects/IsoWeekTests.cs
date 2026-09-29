using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class IsoWeekTests
{
    [Fact]
    public void Constructor_ValidWeek_CreatesExpectedIsoWeek()
    {
        var week = new IsoWeek(2026, 1);

        Assert.Equal(2026, week.Year);
        Assert.Equal(1, week.WeekNumber);
        Assert.Equal("2026-W01", week.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidYear_ThrowsArgumentOutOfRangeException(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoWeek(year, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(54)]
    public void Constructor_WeekNumberOutsideRange_ThrowsArgumentOutOfRangeException(
        int weekNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IsoWeek(2026, weekNumber));
    }

    [Fact]
    public void Constructor_Week53InYearWith52Weeks_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoWeek(2025, 53));
    }

    [Fact]
    public void Constructor_Week53InYearWith53Weeks_Succeeds()
    {
        var week = new IsoWeek(2020, 53);

        Assert.Equal(2020, week.Year);
        Assert.Equal(53, week.WeekNumber);
    }

    [Fact]
    public void GetMonday_ReturnsMondayOfIsoWeek()
    {
        var week = new IsoWeek(2026, 1);

        Assert.Equal(new DateOnly(2025, 12, 29), week.GetMonday());
    }
}