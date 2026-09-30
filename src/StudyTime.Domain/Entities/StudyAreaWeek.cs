using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeek
{
    public Guid Id { get; }
    public DateOnly WeekStartDate { get; }
    public Guid StudyAreaId { get; }
    public Guid StudyPlanId { get; }
    public Guid WeeklyAssessmentId { get; }

    public StudyAreaWeek(
        DateOnly weekStartDate,
        Guid studyAreaId,
        Guid studyPlanId,
        Guid weeklyAssessmentId)
        : this(
            Guid.NewGuid(),
            weekStartDate,
            studyAreaId,
            studyPlanId,
            weeklyAssessmentId)
    {
    }

    public StudyAreaWeek(
        Guid id,
        DateOnly weekStartDate,
        Guid studyAreaId,
        Guid studyPlanId,
        Guid weeklyAssessmentId)
    {
        if (id == Guid.Empty)
            throw new DomainException("StudyAreaWeek id cannot be empty.");

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new DomainException("StudyAreaWeek must start on a Monday.");

        if (studyAreaId == Guid.Empty)
            throw new DomainException("StudyAreaId cannot be empty.");

        if (studyPlanId == Guid.Empty)
            throw new DomainException("StudyPlanId cannot be empty.");

        if (weeklyAssessmentId == Guid.Empty)
            throw new DomainException("WeeklyAssessmentId cannot be empty.");

        Id = id;
        WeekStartDate = weekStartDate;
        StudyAreaId = studyAreaId;
        StudyPlanId = studyPlanId;
        WeeklyAssessmentId = weeklyAssessmentId;
    }
}