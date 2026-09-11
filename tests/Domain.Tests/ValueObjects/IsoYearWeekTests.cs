using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class IsoYearWeekTests
{
    [Theory]
    //[InlineData(2026, 12, 28, 2027, 1)]
    [InlineData(2026, 12, 28, 2026, 53)]
    [InlineData(2027, 1, 3, 2026, 53)]
    [InlineData(2025, 12, 29, 2026, 1)]
    [Trait("Rule", "R21")]
    public void From_DateNearIsoYearBoundary_ReturnsExpectedIsoYearAndWeek(
        int year,
        int month,
        int day,
        int expectedYear,
        int expectedWeek)
    {
        var result = IsoYearWeek.From(new DateOnly(year, month, day));

        Assert.Equal(
            new IsoYearWeekExpectation(expectedYear, expectedWeek),
            new IsoYearWeekExpectation(result.Year, result.WeekNumber));
    }

    [Theory]
    [InlineData(2026, 1)]
    [InlineData(2026, 53)]
    [InlineData(2027, 1)]
    [Trait("Rule", "R21")]
    public void From_ValidYearAndWeek_ReturnsExpectedValue(
        int year,
        int week)
    {
        var result = IsoYearWeek.From(year, week);

        Assert.Equal(
            new IsoYearWeekExpectation(year, week),
            new IsoYearWeekExpectation(result.Year, result.WeekNumber));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 54)]
    [Trait("Rule", "R21")]
    public void From_InvalidYearOrWeek_ThrowsDomainRuleViolationException(
        int year,
        int week)
    {
        Assert.ThrowsAny<Exception>(() => IsoYearWeek.From(year, week));
    }

    [Fact]
    public void GetMonday_ValidIsoWeek_ReturnsMonday()
    {
        var isoWeek = IsoYearWeek.From(2027, 1);
        var result = isoWeek.GetMonday();
        //Assert.Equal(new DateOnly(2026, 12, 28), result);
        Assert.Equal(new DateOnly(2027, 1, 4), result);
    }

    private readonly record struct IsoYearWeekExpectation(int Year, int WeekNumber);
}