using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyPlanTests
{
    [Fact]
    public void Create_ValidData_ReturnsPlan()
    {
        var plan = StudyPlan.Create("Plano A", 0.50m);
        Assert.Equal("Plano A", plan.Name);
    }

    [Fact]
    public void Create_NameWithSpaces_TrimsName()
    {
        var plan = StudyPlan.Create("  Plano A  ", 0.50m);
        Assert.Equal("Plano A", plan.Name);
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void Create_ZeroCoefficient_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyPlan.Create("Plano A", 0m));
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void Create_NegativeCoefficient_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyPlan.Create("Plano A", -0.01m));
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void Create_MinimumPositiveCoefficient_AcceptsCoefficient()
    {
        var plan = StudyPlan.Create("Plano A", 0.01m);
        Assert.Equal(0.01m, plan.Coefficient);
    }

    [Fact]
    [Trait("Rule", "R04")]
    public void Create_DefaultStatus_IsActive()
    {
        var plan = StudyPlan.Create("Plano A", 0.50m);
        Assert.Equal(StudyPlanStatus.Active, plan.Status);
    }

    [Fact]
    [Trait("Rule", "R04")]
    public void Create_InactiveStatus_PreservesStatus()
    {
        var plan = StudyPlan.Create("Plano A", 0.50m, StudyPlanStatus.Inactive);
        Assert.Equal(StudyPlanStatus.Inactive, plan.Status);
    }
}