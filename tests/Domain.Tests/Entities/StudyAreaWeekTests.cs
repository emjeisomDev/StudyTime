using Xunit;
using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaWeekTests
{
    private static readonly DateOnly Monday = new(2026, 9, 7);

    [Fact]
    public void Create_ValidData_ReturnsStudyAreaWeek()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();

        var week = StudyAreaWeek.Create(Monday, areaId, planId, assessmentId);
        Assert.Equal(Monday, week.WeekStartDate);
    }

    [Fact]
    public void Create_ValidData_PreservesAreaIdentifier()
    {
        var areaId = Guid.NewGuid();
        var week = StudyAreaWeek.Create(Monday, areaId, Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(areaId, week.StudyAreaId);
    }

    [Fact]
    public void Create_EmptyAreaId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(
                Monday,
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyPlanId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(
                Monday,
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyAssessmentId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyAreaWeek.Create(
                Monday,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty));
    }

    [Fact]
    [Trait("Rule", "R07")]
    public void Create_NonMonday_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() =>
            StudyAreaWeek.Create(
                Monday.AddDays(1),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()));
    }
}