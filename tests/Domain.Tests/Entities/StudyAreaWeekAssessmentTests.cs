using Xunit;
using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaWeekAssessmentTests
{
    private static readonly Guid WeekId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ReturnsAssessment()
    {
        var assessment = StudyAreaWeekAssessment.Create(100m, WeekId);
        Assert.Equal(100m, assessment.WeekIndividualGoal);
    }

    [Fact]
    public void Create_ZeroGoal_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyAreaWeekAssessment.Create(0m, WeekId));
    }

    [Fact]
    public void Create_NegativeGoal_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyAreaWeekAssessment.Create(-1m, WeekId));
    }

    [Fact]
    public void Create_NegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyAreaWeekAssessment.Create(100m, WeekId, -1));
    }

    [Fact]
    public void Create_EmptyWeekId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => StudyAreaWeekAssessment.Create(100m, Guid.Empty));
    }

    [Fact]
    public void Create_ValidMinutes_PreservesMinutes()
    {
        var assessment = StudyAreaWeekAssessment.Create(100m, WeekId, 45);
        Assert.Equal(45, assessment.MinutesStudied);
    }
}