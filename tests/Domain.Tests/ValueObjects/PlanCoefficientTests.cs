using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class PlanCoefficientTests
{
    [Fact]
    [Trait("Rule", "R13")]
    public void From_Zero_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => PlanCoefficient.From(0m));
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void From_Negative_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => PlanCoefficient.From(-0.01m));
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void From_ZeroPointZeroOne_AcceptsValue()
    {
        var result = PlanCoefficient.From(0.01m);
        Assert.Equal(0.01m, result.Value);
    }

    [Fact]
    public void From_PositiveValue_PreservesValue()
    {
        var result = PlanCoefficient.From(0.33m);
        Assert.Equal(0.33m, result.Value);
    }
}