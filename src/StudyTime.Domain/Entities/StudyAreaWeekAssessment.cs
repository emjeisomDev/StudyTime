namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeekAssessment
{
    public Guid Id { get; private set; }
    public decimal WeekIndividualGoal { get; private set; }
    public int MinutesStudied { get; private set; }
    public Guid StudyAreaWeekId { get; private set; }
    public StudyAreaWeek StudyAreaWeek { get; private set; } = null!;


    private StudyAreaWeekAssessment() { }

    private StudyAreaWeekAssessment(Guid id, decimal weekIndividualGoal, Guid studyAreaWeekId, int minutesStudied)
    {
        Id = id;
        WeekIndividualGoal = weekIndividualGoal;
        MinutesStudied = minutesStudied;
        StudyAreaWeekId = studyAreaWeekId;
    }

    public static StudyAreaWeekAssessment Create(
        decimal weekIndividualGoal,
        Guid studyAreaWeekId,
        int minutesStudied = 0,
        Guid? id = null)
    {
        if (weekIndividualGoal <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weekIndividualGoal),
                "The weekly individual goal must be greater than zero.");
        }

        if (minutesStudied < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minutesStudied),
                "The minutes studied cannot be negative.");
        }

        if (studyAreaWeekId == Guid.Empty)
        {
            throw new ArgumentException(
                "The weekly configuration identifier is required.",
                nameof(studyAreaWeekId));
        }

        return new StudyAreaWeekAssessment(
            id ?? Guid.NewGuid(),
            weekIndividualGoal,
            studyAreaWeekId,
            minutesStudied);
    }
}