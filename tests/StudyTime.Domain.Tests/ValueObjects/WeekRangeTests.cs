using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class WeekRangeTests
{
    [Fact]
    public void Constructor_MondayAtMidnight_CreatesExpectedWeekRange()
    {
        var start = new DateTime(2026, 8, 31, 0, 0, 0);
        var range = new WeekRange(start);

        Assert.Equal(start, range.Start);
        Assert.Equal(new DateTime(2026, 9, 6, 23, 59, 59), range.End);
    }

    [Fact]
    public void Constructor_NonMonday_ThrowsArgumentException()
    {
        var start = new DateTime(2026, 9, 1, 0, 0, 0);

        Assert.Throws<ArgumentException>(() => new WeekRange(start));
    }

    [Fact]
    public void Constructor_MondayNotAtMidnight_ThrowsArgumentException()
    {
        var start = new DateTime(2026, 8, 31, 1, 0, 0);

        Assert.Throws<ArgumentException>(() => new WeekRange(start));
    }

    [Fact]
    public void Contains_ValueAtStart_ReturnsTrue()
    {
        var range = new WeekRange(new DateTime(2026, 8, 31, 0, 0, 0));

        Assert.True(range.Contains(new DateTime(2026, 8, 31, 0, 0, 0)));
    }

    [Fact]
    public void Contains_ValueAtEnd_ReturnsTrue()
    {
        var range = new WeekRange(new DateTime(2026, 8, 31, 0, 0, 0));

        Assert.True(range.Contains(new DateTime(2026, 9, 6, 23, 59, 59)));
    }

    [Fact]
    public void Contains_ValueBeforeStart_ReturnsFalse()
    {
        var range = new WeekRange(new DateTime(2026, 8, 31, 0, 0, 0));

        Assert.False(range.Contains(new DateTime(2026, 8, 30, 23, 59, 59)));
    }

    [Fact]
    public void Contains_ValueAfterEnd_ReturnsFalse()
    {
        var range = new WeekRange(new DateTime(2026, 8, 31, 0, 0, 0));

        Assert.False(range.Contains(new DateTime(2026, 9, 7, 0, 0, 0)));
    }

    [Fact]
    public void Constructor_DateOnlyMonday_CreatesExpectedWeekRange()
    {
        var range = new WeekRange(new DateOnly(2026, 8, 31));

        Assert.Equal(new DateTime(2026, 8, 31, 0, 0, 0), range.Start);
        Assert.Equal(new DateTime(2026, 9, 6, 23, 59, 59), range.End);
    }
}