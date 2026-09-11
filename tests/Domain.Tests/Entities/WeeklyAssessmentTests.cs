using Xunit;
using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Tests.Entities;

public sealed class WeeklyAssessmentTests
{
    [Fact]
    public void Create_ValidIsoWeek_ReturnsAssessment()
    {
        var assessment = WeeklyAssessment.Create(1, 2027, 1500m);
        Assert.Equal(1, assessment.WeekNumber);
    }

    [Fact]
    public void Create_ValidIsoWeek_PreservesYear()
    {
        var assessment = WeeklyAssessment.Create(1, 2027, 1500m);
        Assert.Equal(2027, assessment.Year);
    }

    [Fact]
    public void Create_ValidGoal_PreservesGlobalGoal()
    {
        var assessment = WeeklyAssessment.Create(1, 2027, 1500m);
        Assert.Equal(1500m, assessment.WeekGlobalGoal);
    }

    [Fact]
    public void Create_ZeroGlobalGoal_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyAssessment.Create(1, 2027, 0m));
    }

    [Fact]
    public void Create_NegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyAssessment.Create(1, 2027, 1500m, -1));
    }

    [Fact]
    public void Create_InvalidWeekNumber_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => WeeklyAssessment.Create(54, 2027, 1500m));
    }
}