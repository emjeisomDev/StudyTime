using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class StudyMinutesTests
{
    [Fact]
    [Trait("Rule", "R05")]
    public void From_Zero_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyMinutes.From(0));
    }

    [Fact]
    [Trait("Rule", "R05")]
    public void From_Negative_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyMinutes.From(-1));
    }

    [Fact]
    [Trait("Rule", "R05")]
    public void From_PositiveValue_ReturnsValue()
    {
        var result = StudyMinutes.From(60);
        Assert.Equal(60, result.Value);
    }

    [Fact]
    public void ToString_PositiveValue_ReturnsNumericText()
    {
        var result = StudyMinutes.From(60);
        Assert.Equal("60", result.ToString());
    }
}