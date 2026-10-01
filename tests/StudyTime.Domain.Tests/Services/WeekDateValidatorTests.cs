using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class WeekDateValidatorTests
{
    private static readonly DateOnly WeekStartDate = new(2026, 9, 28);

    [Theory]
    [InlineData(2026, 9, 28)]
    [InlineData(2026, 9, 29)]
    [InlineData(2026, 9, 30)]
    [InlineData(2026, 10, 1)]
    [InlineData(2026, 10, 2)]
    [InlineData(2026, 10, 3)]
    [InlineData(2026, 10, 4)]
    public void IsDateWithinWeek_WhenDateIsWithinWeek_ReturnsTrue(
        int year,
        int month,
        int day)
    {
        DateOnly date = new(year, month, day);

        bool result = WeekDateValidator.IsDateWithinWeek(date, WeekStartDate);

        Assert.True(result);
    }

    [Theory]
    [InlineData(2026, 9, 27)]
    [InlineData(2026, 10, 5)]
    public void IsDateWithinWeek_WhenDateIsOutsideWeek_ReturnsFalse(int year, int month, int day)
    {
        DateOnly date = new(year, month, day);
        bool result = WeekDateValidator.IsDateWithinWeek(date, WeekStartDate);
        Assert.False(result);
    }

    [Fact]
    public void ValidateDate_WhenDateIsWithinWeek_DoesNotThrow()
    {
        DateOnly date = new(2026, 10, 4);

        WeekDateValidator.ValidateDate(date, WeekStartDate);
    }

    [Fact]
    public void ValidateDate_WhenDateIsBeforeWeek_ThrowsArgumentOutOfRangeException()
    {
        DateOnly date = new(2026, 9, 27);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WeekDateValidator.ValidateDate(date, WeekStartDate));
    }

    [Fact]
    public void ValidateDate_WhenDateIsAfterWeek_ThrowsArgumentOutOfRangeException()
    {
        DateOnly date = new(2026, 10, 5);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WeekDateValidator.ValidateDate(date, WeekStartDate));
    }

    [Theory]
    [InlineData(1500)]
    [InlineData(1800)]
    public void IsValidConfiguration_WhenGoalIsAtLeastMinimum_ReturnsTrue(decimal goal)
    {
        Assert.True(WeekDateValidator.IsValidConfiguration(goal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1499)]
    [InlineData(1499.99)]
    public void IsValidConfiguration_WhenGoalIsBelowMinimum_ReturnsFalse(decimal goal)
    {
        Assert.False(WeekDateValidator.IsValidConfiguration(goal));
    }

    [Fact]
    public void ValidateConfiguration_WhenGoalIsAtLeastMinimum_DoesNotThrow()
    {
        WeekDateValidator.ValidateConfiguration(1500m);
    }

    [Fact]
    public void ValidateConfiguration_WhenGoalIsBelowMinimum_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => WeekDateValidator.ValidateConfiguration(1499m));
    }

    [Fact]
    public void ValidateRecord_WhenDateAndConfigurationAreValid_DoesNotThrow()
    {
        DateOnly date = new(2026, 10, 1);
        WeekDateValidator.ValidateRecord(date, WeekStartDate, 1500m);
    }

    [Fact]
    public void ValidateRecord_WhenDateIsOutsideWeek_ThrowsArgumentOutOfRangeException()
    {
        DateOnly date = new(2026, 10, 5);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WeekDateValidator.ValidateRecord(
                date,
                WeekStartDate,
                1500m));
    }

    [Fact]
    public void ValidateRecord_WhenConfigurationIsInvalid_ThrowsArgumentException()
    {
        DateOnly date = new(2026, 10, 1);

        Assert.Throws<ArgumentException>(
            () => WeekDateValidator.ValidateRecord(
                date,
                WeekStartDate,
                1499m));
    }
}