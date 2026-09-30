using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyPlanTests
{
    [Fact]
    public void Constructor_ValidData_CreatesActiveStudyPlan()
    {
        var id = Guid.NewGuid();
        var coefficient = new Coefficient(1.5m);

        var studyPlan = new StudyPlan(id, "Standard", coefficient);

        Assert.Equal(id, studyPlan.Id);
        Assert.Equal("Standard", studyPlan.Name);
        Assert.Equal(coefficient, studyPlan.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, studyPlan.Status);
    }

    [Fact]
    public void Constructor_WithoutId_GeneratesId()
    {
        var studyPlan = new StudyPlan(
            "Standard",
            new Coefficient(1.5m));

        Assert.NotEqual(Guid.Empty, studyPlan.Id);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyPlan(
                Guid.Empty,
                "Standard",
                new Coefficient(1.5m)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_EmptyName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new StudyPlan(name, new Coefficient(1.5m)));
    }

    [Fact]
    public void Constructor_NameLongerThan80Characters_ThrowsDomainException()
    {
        var name = new string('A', 81);

        Assert.Throws<DomainException>(() =>
            new StudyPlan(name, new Coefficient(1.5m)));
    }

    [Fact]
    public void Constructor_NameWith80Characters_CreatesStudyPlan()
    {
        var name = new string('A', 80);

        var studyPlan = new StudyPlan(name, new Coefficient(1.5m));

        Assert.Equal(name, studyPlan.Name);
    }

    [Fact]
    public void Constructor_NullCoefficient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StudyPlan("Standard", null!));
    }

    [Fact]
    public void ChangeStatus_Inactive_ChangesStatusToInactive()
    {
        var studyPlan = new StudyPlan(
            "Standard",
            new Coefficient(1.5m));

        studyPlan.ChangeStatus(StudyPlanStatus.Inactive);

        Assert.Equal(StudyPlanStatus.Inactive, studyPlan.Status);
    }

    [Fact]
    public void ChangeStatus_ActiveAfterInactive_ChangesStatusToActive()
    {
        var studyPlan = new StudyPlan(
            "Standard",
            new Coefficient(1.5m));

        studyPlan.ChangeStatus(StudyPlanStatus.Inactive);
        studyPlan.ChangeStatus(StudyPlanStatus.Active);

        Assert.Equal(StudyPlanStatus.Active, studyPlan.Status);
    }

    [Fact]
    public void ChangeStatus_UndefinedValue_ThrowsDomainException()
    {
        var studyPlan = new StudyPlan(
            "Standard",
            new Coefficient(1.5m));

        Assert.Throws<DomainException>(() =>
            studyPlan.ChangeStatus((StudyPlanStatus)999));
    }
}