using StudyTime.Domain.Enums;
using Xunit;

namespace StudyTime.Domain.Tests.Enums;

public sealed class StudyPlanStatusTests
{
    [Theory]
    [InlineData("active")]
    [InlineData("ACTIVE")]
    [InlineData("Active")]
    [Trait("Rule", "R04")]
    public void Parse_ActiveValue_ReturnsActive(string value)
    {
        var result = StudyPlanStatusExtensions.Parse(value);
        Assert.Equal(StudyPlanStatus.Active, result);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("INACTIVE")]
    [InlineData("Inactive")]
    [Trait("Rule", "R04")]
    public void Parse_InactiveValue_ReturnsInactive(string value)
    {
        var result = StudyPlanStatusExtensions.Parse(value);
        Assert.Equal(StudyPlanStatus.Inactive, result);
    }

    [Fact]
    [Trait("Rule", "R04")]
    public void Parse_PortugueseActiveValue_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyPlanStatusExtensions.Parse("ativo"));
    }

    [Fact]
    [Trait("Rule", "R04")]
    public void Parse_PortugueseInactiveValue_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyPlanStatusExtensions.Parse("inativo"));
    }

    [Fact]
    [Trait("Rule", "R04")]
    public void Parse_UnknownValue_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyPlanStatusExtensions.Parse("enabled"));
    }

    [Fact]
    public void ToStorageValue_Active_ReturnsLowercaseActive()
    {
        var result = StudyPlanStatus.Active.ToStorageValue();
        Assert.Equal("active", result);
    }

    [Fact]
    public void ToStorageValue_Inactive_ReturnsLowercaseInactive()
    {
        var result = StudyPlanStatus.Inactive.ToStorageValue();
        Assert.Equal("inactive", result);
    }
}