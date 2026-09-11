using Xunit;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class WeekPolicyServiceTests
{
    private readonly WeekPolicyService _sut = new();
    private static readonly DateOnly Monday = new(2026, 9, 7);
    private static readonly DateOnly Sunday = new(2026, 9, 13);

    [Fact]
    [Trait("Rule", "R11")]
    public void EnsureWeekHasMinimumGoal_1499Point99_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => _sut.EnsureWeekHasMinimumGoal([1499.99m]));
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void EnsureWeekHasMinimumGoal_1500Point00_DoesNotThrow()
    {
        _sut.EnsureWeekHasMinimumGoal([1500.00m]);
        Assert.True(true);
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void IsWeekValidForRecords_1499Point99_ReturnsFalse()
    {
        var result = _sut.IsWeekValidForRecords([1499.99m]);
        Assert.False(result);
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void IsWeekValidForRecords_1500Point00_ReturnsTrue()
    {
        var result = _sut.IsWeekValidForRecords([1500.00m]);
        Assert.True(result);
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void IsWeekValidForRecords_TwoGoals_ReturnsBasedOnSum()
    {
        var result = _sut.IsWeekValidForRecords([1000m, 500m]);
        Assert.True(result);
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void IsWeekValidForRecords_EmptyCollection_ReturnsFalse()
    {
        var result = _sut.IsWeekValidForRecords([]);
        Assert.False(result);
    }

    [Fact]
    [Trait("Rule", "R07")]
    public void IsWeekStartOnMonday_Monday_ReturnsTrue()
    {
        var result = _sut.IsWeekStartOnMonday(Monday);
        Assert.True(result);
    }

    [Fact]
    [Trait("Rule", "R07")]
    public void IsWeekStartOnMonday_Sunday_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => _sut.IsWeekStartOnMonday(Sunday));
    }

    [Fact]
    [Trait("Rule", "R06")]
    public void GetSundayOf_Monday_ReturnsSunday()
    {
        var result = _sut.GetSundayOf(Monday);
        Assert.Equal(Sunday, result);
    }

    [Theory]
    [InlineData(2026, 9, 7, true)]
    [InlineData(2026, 9, 13, true)]
    [InlineData(2026, 9, 6, false)]
    [InlineData(2026, 9, 14, false)]
    [Trait("Rule", "R06")]
    public void IsDateWithinWeek_DatePosition_ReturnsExpectedResult(
        int year,
        int month,
        int day,
        bool expected)
    {
        var result = _sut.IsDateWithinWeek(
            new DateOnly(year, month, day),
            Monday);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Rule", "R31")]
    public void IsWeekValidForRecords_AndIsDateWithinWeek_ValidConfigurationAndDate_ReturnsTrue()
    {
        var goals = new[] {800m, 700m};

        var validConfiguration = _sut.IsWeekValidForRecords(goals);
        var validDate = _sut.IsDateWithinWeek(
            new DateOnly(2026, 9, 10),
            Monday);

        Assert.True(validConfiguration && validDate);
    }

    [Fact]
    [Trait("Rule", "R31")]
    public void IsWeekValidForRecords_AndIsDateWithinWeek_InvalidConfiguration_ReturnsFalse()
    {
        var goals = new[] {800m, 699.99m};

        var validConfiguration = _sut.IsWeekValidForRecords(goals);
        var validDate = _sut.IsDateWithinWeek(
            new DateOnly(2026, 9, 10),
            Monday);

        Assert.False(validConfiguration && validDate);
    }
}