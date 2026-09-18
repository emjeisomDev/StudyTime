using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeek
{
    public Guid Id { get; init; }
    public DateOnly WeekStartDate { get; init; }
    public Guid StudyAreaId { get; private set; }
    public Guid StudyPlanId { get; private set; }
    public Guid WeeklyAssessmentId { get; init; }

    public WeekRange WeekRange => WeekRange.FromStartDate(WeekStartDate);

    public StudyAreaWeek(
        Guid id,
        DateOnly weekStartDate,
        Guid studyAreaId,
        Guid studyPlanId,
        Guid weeklyAssessmentId)
    {
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainValidationException("Study area week start date must be a Monday.");
        }

        Id = id;
        WeekStartDate = weekStartDate;
        StudyAreaId = studyAreaId;
        StudyPlanId = studyPlanId;
        WeeklyAssessmentId = weeklyAssessmentId;
    }

    public void UpdateConfiguration(Guid studyAreaId, Guid studyPlanId)
    {
        StudyAreaId = studyAreaId;
        StudyPlanId = studyPlanId;
    }
}