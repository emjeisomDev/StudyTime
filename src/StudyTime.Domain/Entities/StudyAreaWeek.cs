using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeek
{
    public Guid Id { get; private set; }
    public DateOnly WeekStartDate { get; private set; }
    public Guid StudyAreaId { get; private set; }
    public Guid StudyPlanId { get; private set; }
    public Guid WeeklyAssessmentId { get; private set; }
    public StudyArea StudyArea { get; private set; } = null!;
    public StudyPlan StudyPlan { get; private set; } = null!;
    public WeeklyAssessment WeeklyAssessment { get; private set; } = null!;
    public StudyAreaWeekAssessment? Assessment { get; private set; }
    public ICollection<StudyRecord> StudyRecords { get; private set; } = new List<StudyRecord>();

    private StudyAreaWeek() { }

    private StudyAreaWeek(
        Guid id,
        WeekStartDate weekStartDate,
        Guid studyAreaId,
        Guid studyPlanId,
        Guid weeklyAssessmentId)
    {
        Id = id;
        WeekStartDate = weekStartDate.Value;
        StudyAreaId = studyAreaId;
        StudyPlanId = studyPlanId;
        WeeklyAssessmentId = weeklyAssessmentId;
    }


    public static StudyAreaWeek Create(
        DateOnly weekStartDate,
        Guid studyAreaId,
        Guid studyPlanId,
        Guid weeklyAssessmentId,
        Guid? id = null)
    {
        if (studyAreaId == Guid.Empty)
        {
            throw new ArgumentException(
                "The field of study identifier is required.",
                nameof(studyAreaId));
        }

        if (studyPlanId == Guid.Empty)
        {
            throw new ArgumentException(
                "The curriculum identifier is required.",
                nameof(studyPlanId));
        }

        if (weeklyAssessmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "The weekly evaluation ID is required.",
                nameof(weeklyAssessmentId));
        }

        var validWeekStartDate = ValueObjects.WeekStartDate.From(weekStartDate);

        return new StudyAreaWeek(
            id ?? Guid.NewGuid(),
            validWeekStartDate,
            studyAreaId,
            studyPlanId,
            weeklyAssessmentId);
    }
}