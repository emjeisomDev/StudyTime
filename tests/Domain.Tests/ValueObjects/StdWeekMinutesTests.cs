using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class StdWeekMinutesTests
{
    [Fact]
    [Trait("Rule", "R03")]
    public void From_Zero_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StdWeekMinutes.From(0));
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void From_Negative_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StdWeekMinutes.From(-1));
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void From_PositiveValue_ReturnsValue()
    {
        var result = StdWeekMinutes.From(120);
        Assert.Equal(120, result.Value);
    }
}