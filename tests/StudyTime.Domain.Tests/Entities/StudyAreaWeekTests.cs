using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaWeekTests
{
    private static readonly DateOnly Monday = new(2026, 9, 28);
    private static readonly Guid StudyAreaId = Guid.NewGuid();
    private static readonly Guid StudyPlanId = Guid.NewGuid();
    private static readonly Guid WeeklyAssessmentId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidData_CreatesStudyAreaWeek()
    {
        var entity = new StudyAreaWeek(
            Monday,
            StudyAreaId,
            StudyPlanId,
            WeeklyAssessmentId);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(Monday, entity.WeekStartDate);
        Assert.Equal(StudyAreaId, entity.StudyAreaId);
        Assert.Equal(StudyPlanId, entity.StudyPlanId);
        Assert.Equal(WeeklyAssessmentId, entity.WeeklyAssessmentId);
    }

    [Fact]
    public void Constructor_WithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var entity = new StudyAreaWeek(
            id,
            Monday,
            StudyAreaId,
            StudyPlanId,
            WeeklyAssessmentId);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyAreaWeek(
                Guid.Empty,
                Monday,
                StudyAreaId,
                StudyPlanId,
                WeeklyAssessmentId));
    }

    [Theory]
    [InlineData(2026, 9, 27)]
    [InlineData(2026, 9, 29)]
    [InlineData(2026, 10, 4)]
    public void Constructor_DateNotMonday_ThrowsDomainException(
        int year,
        int month,
        int day)
    {
        var date = new DateOnly(year, month, day);

        Assert.Throws<DomainException>(() =>
            new StudyAreaWeek(
                date,
                StudyAreaId,
                StudyPlanId,
                WeeklyAssessmentId));
    }

    [Fact]
    public void Constructor_EmptyStudyAreaId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyAreaWeek(
                Monday,
                Guid.Empty,
                StudyPlanId,
                WeeklyAssessmentId));
    }

    [Fact]
    public void Constructor_EmptyStudyPlanId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyAreaWeek(
                Monday,
                StudyAreaId,
                Guid.Empty,
                WeeklyAssessmentId));
    }

    [Fact]
    public void Constructor_EmptyWeeklyAssessmentId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyAreaWeek(
                Monday,
                StudyAreaId,
                StudyPlanId,
                Guid.Empty));
    }

    [Fact]
    public void Constructor_SameAreaAndWeek_AllowsSeparateInstances()
    {
        var first = new StudyAreaWeek(
            Monday,
            StudyAreaId,
            StudyPlanId,
            WeeklyAssessmentId);

        var second = new StudyAreaWeek(
            Monday,
            StudyAreaId,
            StudyPlanId,
            WeeklyAssessmentId);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.StudyAreaId, second.StudyAreaId);
        Assert.Equal(first.WeekStartDate, second.WeekStartDate);
    }
}