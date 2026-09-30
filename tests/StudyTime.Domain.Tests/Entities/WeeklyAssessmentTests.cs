using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Tests.Entities;

public sealed class WeeklyAssessmentTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesWeeklyAssessment()
    {
        var assessment = new WeeklyAssessment(
            weekNumber: 36,
            year: 2026,
            weekGlobalGoal: 1500m,
            minutesStudied: 0);

        Assert.NotEqual(Guid.Empty, assessment.Id);
        Assert.Equal(36, assessment.WeekNumber);
        Assert.Equal(2026, assessment.Year);
        Assert.Equal(1500m, assessment.WeekGlobalGoal);
        Assert.Equal(0, assessment.MinutesStudied);
    }

    [Fact]
    public void Constructor_ExplicitId_PreservesId()
    {
        Guid id = Guid.NewGuid();

        var assessment = new WeeklyAssessment(
            id,
            weekNumber: 36,
            year: 2026,
            weekGlobalGoal: 1500m,
            minutesStudied: 120);

        Assert.Equal(id, assessment.Id);
        Assert.Equal(36, assessment.WeekNumber);
        Assert.Equal(2026, assessment.Year);
        Assert.Equal(1500m, assessment.WeekGlobalGoal);
        Assert.Equal(120, assessment.MinutesStudied);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => new WeeklyAssessment(
                Guid.Empty,
                36,
                2026,
                1500m,
                0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(54)]
    public void Constructor_InvalidWeekNumber_ThrowsDomainException(
        int weekNumber)
    {
        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => new WeeklyAssessment(
                weekNumber,
                2026,
                1500m,
                0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidYear_ThrowsDomainException(int year)
    {
        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => new WeeklyAssessment(
                36,
                year,
                1500m,
                0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveGlobalGoal_ThrowsDomainException(
        decimal globalGoal)
    {
        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => new WeeklyAssessment(
                36,
                2026,
                globalGoal,
                0));
    }

    [Fact]
    public void Constructor_NegativeMinutesStudied_ThrowsDomainException()
    {
        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => new WeeklyAssessment(
                36,
                2026,
                1500m,
                -1));
    }

    [Fact]
    public void UpdateGlobalGoal_ValidValue_UpdatesGoal()
    {
        var assessment = new WeeklyAssessment(
            36,
            2026,
            1500m,
            0);

        assessment.UpdateGlobalGoal(1800m);

        Assert.Equal(1800m, assessment.WeekGlobalGoal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateGlobalGoal_NonPositiveValue_ThrowsDomainException(
        decimal globalGoal)
    {
        var assessment = new WeeklyAssessment(
            36,
            2026,
            1500m,
            0);

        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => assessment.UpdateGlobalGoal(globalGoal));
    }

    [Fact]
    public void UpdateMinutesStudied_NonNegativeValue_UpdatesMinutes()
    {
        var assessment = new WeeklyAssessment(
            36,
            2026,
            1500m,
            0);

        assessment.UpdateMinutesStudied(300);

        Assert.Equal(300, assessment.MinutesStudied);
    }

    [Fact]
    public void UpdateMinutesStudied_NegativeValue_ThrowsDomainException()
    {
        var assessment = new WeeklyAssessment(
            36,
            2026,
            1500m,
            0);

        Assert.Throws<StudyTime.Domain.Exceptions.DomainException>(
            () => assessment.UpdateMinutesStudied(-1));
    }
}