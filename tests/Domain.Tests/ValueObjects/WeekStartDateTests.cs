using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class WeekStartDateTests
{
    [Fact]
    [Trait("Rule", "R07")]
    public void From_Monday_ReturnsWeekStartDate()
    {
        var monday = new DateOnly(2026, 9, 7);
        var result = WeekStartDate.From(monday);
        Assert.Equal(monday, result.Value);
    }

    [Theory]
    [InlineData(2026, 9, 8)]
    [InlineData(2026, 9, 9)]
    [InlineData(2026, 9, 10)]
    [InlineData(2026, 9, 11)]
    [InlineData(2026, 9, 12)]
    [InlineData(2026, 9, 13)]
    [Trait("Rule", "R07")]
    public void From_NonMonday_ThrowsDomainRuleViolationException(int year, int month, int day)
    {
        Assert.ThrowsAny<Exception>(() => WeekStartDate.From(new DateOnly(year, month, day)));
    }

    [Fact]
    public void ToString_Monday_ReturnsIsoDate()
    {
        var result = WeekStartDate.From(new DateOnly(2026, 9, 7));
        Assert.Equal("2026-09-07", result.ToString());
    }
}