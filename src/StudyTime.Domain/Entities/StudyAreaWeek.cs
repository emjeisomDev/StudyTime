namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeek
{
    public Guid Id { get; private set; }
    public DateOnly WeekStartDate { get; private set; }
    public Guid StudyAreaId { get; private set; }
    public Guid StudyPlanId { get; private set; }
    public Guid WeeklyAssessmentId { get; private set; }
    public StudyAreaWeekAssessment Assessment { get; private set; }

    private StudyAreaWeek()
    {
        Assessment = null!;
    }

    private StudyAreaWeek(Guid id, DateOnly weekStartDate, StudyArea studyArea, StudyPlan studyPlan, Guid weeklyAssessmentId, decimal weekIndividualGoal)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The study area week id must not be empty.", nameof(id));
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("The week start date must be a Monday.", nameof(weekStartDate));
        ArgumentNullException.ThrowIfNull(studyArea);
        ArgumentNullException.ThrowIfNull(studyPlan);
        if (studyPlan.Status != Enums.StudyPlanStatus.Active)
            throw new InvalidOperationException("An inactive study plan cannot be used in a study area week.");
        if (weeklyAssessmentId == Guid.Empty)
            throw new ArgumentException("The weekly assessment id must not be empty.", nameof(weeklyAssessmentId));
        if (weekIndividualGoal <= 0)
            throw new ArgumentOutOfRangeException(nameof(weekIndividualGoal), "The individual weekly goal must be greater than zero.");

        Id = id;
        WeekStartDate = weekStartDate;
        StudyAreaId = studyArea.Id;
        StudyPlanId = studyPlan.Id;
        WeeklyAssessmentId = weeklyAssessmentId;
        Assessment = StudyAreaWeekAssessment.Create(Id, weekIndividualGoal);
    }

    public static StudyAreaWeek Create(DateOnly weekStartDate, StudyArea studyArea, StudyPlan studyPlan, Guid weeklyAssessmentId, decimal weekIndividualGoal)
        => new(Guid.NewGuid(), weekStartDate, studyArea, studyPlan, weeklyAssessmentId, weekIndividualGoal);

    public static StudyAreaWeek Create(Guid id, DateOnly weekStartDate, StudyArea studyArea, StudyPlan studyPlan, Guid weeklyAssessmentId, decimal weekIndividualGoal)
        => new(id, weekStartDate, studyArea, studyPlan, weeklyAssessmentId, weekIndividualGoal);

    public void Reconfigure(StudyArea studyArea, StudyPlan studyPlan, decimal weekIndividualGoal)
    {
        ArgumentNullException.ThrowIfNull(studyArea);
        ArgumentNullException.ThrowIfNull(studyPlan);

        if (studyPlan.Status != Enums.StudyPlanStatus.Active)
            throw new InvalidOperationException("An inactive study plan cannot be used in a study area week.");
        if (weekIndividualGoal <= 0)
            throw new ArgumentOutOfRangeException(nameof(weekIndividualGoal), "The individual weekly goal must be greater than zero.");

        StudyAreaId = studyArea.Id;
        StudyPlanId = studyPlan.Id;
        Assessment.RecalculateGoal(weekIndividualGoal);
    }
}