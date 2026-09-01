using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace Domain.Tests.Entities;

public sealed class StudyPlanTests
{
    [Fact]
    public void CreateShouldGenerateActivePlanByDefault()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal("Normal", plan.Name);
        Assert.Equal(1m, plan.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, plan.Status);
    }

    [Fact]
    public void CreateShouldTrimName()
    {
        var plan = StudyPlan.Create("  Normal  ", 1m);

        Assert.Equal("Normal", plan.Name);
    }

    [Fact]
    public void CreateWithExplicitIdShouldPreserveId()
    {
        var id = Guid.NewGuid();

        var plan = StudyPlan.Create(id, "Normal", 1m);

        Assert.Equal(id, plan.Id);
    }

    [Fact]
    public void CreateShouldAcceptInactiveStatus()
    {
        var plan = StudyPlan.Create("Normal", 1m, StudyPlanStatus.Inactive);

        Assert.Equal(StudyPlanStatus.Inactive, plan.Status);
    }

    [Fact]
    public void CreateShouldRejectEmptyId()
    {
        Assert.Throws<ArgumentException>(() => StudyPlan.Create(Guid.Empty, "Normal", 1m));
    }

    [Fact]
    public void CreateShouldRejectInvalidName()
    {
        Assert.Throws<ArgumentException>(() => StudyPlan.Create(null!, 1m));
        Assert.Throws<ArgumentException>(() => StudyPlan.Create("   ", 1m));
        Assert.Throws<ArgumentException>(() => StudyPlan.Create(new string('A', 81), 1m));
    }

    [Fact]
    public void CreateShouldRejectNonPositiveCoefficient()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyPlan.Create("Normal", 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyPlan.Create("Normal", -0.5m));
    }

    [Fact]
    public void CreateShouldRejectInvalidStatus()
    {
        var invalidStatus = (StudyPlanStatus)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => StudyPlan.Create("Normal", 1m, invalidStatus));
    }

    [Fact]
    public void RenameShouldChangeName()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        plan.Rename("Intensivo");

        Assert.Equal("Intensivo", plan.Name);
    }

    [Fact]
    public void RenameShouldTrimName()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        plan.Rename("  Intensivo  ");

        Assert.Equal("Intensivo", plan.Name);
    }

    [Fact]
    public void RenameShouldRejectInvalidName()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        Assert.Throws<ArgumentException>(() => plan.Rename(""));
        Assert.Throws<ArgumentException>(() => plan.Rename(new string('A', 81)));
    }

    [Fact]
    public void ChangeCoefficientShouldUpdateValue()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        plan.ChangeCoefficient(1.5m);

        Assert.Equal(1.5m, plan.Coefficient);
    }

    [Fact]
    public void ChangeCoefficientShouldRejectNonPositiveValue()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        Assert.Throws<ArgumentOutOfRangeException>(() => plan.ChangeCoefficient(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.ChangeCoefficient(-1m));
    }

    [Fact]
    public void ActivateShouldSetActiveStatus()
    {
        var plan = StudyPlan.Create("Normal", 1m, StudyPlanStatus.Inactive);

        plan.Activate();

        Assert.Equal(StudyPlanStatus.Active, plan.Status);
    }

    [Fact]
    public void DeactivateShouldSetInactiveStatus()
    {
        var plan = StudyPlan.Create("Normal", 1m);

        plan.Deactivate();

        Assert.Equal(StudyPlanStatus.Inactive, plan.Status);
    }
}