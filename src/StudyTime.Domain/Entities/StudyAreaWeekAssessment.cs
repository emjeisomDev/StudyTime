namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeekAssessment
{
    public Guid Id { get; private set; }
    public decimal WeekIndividualGoal { get; private set; }
    public int MinutesStudied { get; private set; }
    public Guid StudyAreaWeekId { get; private set; }

    public bool GoalAchieved => MinutesStudied >= WeekIndividualGoal;

    private StudyAreaWeekAssessment() { }

    private StudyAreaWeekAssessment(Guid id, decimal weekIndividualGoal, Guid studyAreaWeekId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The assessment id must not be empty.", nameof(id));
        if (weekIndividualGoal <= 0)
            throw new ArgumentOutOfRangeException(nameof(weekIndividualGoal), "The individual weekly goal must be greater than zero.");
        if (studyAreaWeekId == Guid.Empty)
            throw new ArgumentException("The study area week id must not be empty.", nameof(studyAreaWeekId));

        Id = id;
        WeekIndividualGoal = weekIndividualGoal;
        StudyAreaWeekId = studyAreaWeekId;
        MinutesStudied = 0;
    }

    public static StudyAreaWeekAssessment Create(Guid studyAreaWeekId, decimal weekIndividualGoal)
        => new(Guid.NewGuid(), weekIndividualGoal, studyAreaWeekId);

    public static StudyAreaWeekAssessment Create(Guid id, Guid studyAreaWeekId, decimal weekIndividualGoal, int minutesStudied = 0)
    {
        if (minutesStudied < 0)
            throw new ArgumentOutOfRangeException(nameof(minutesStudied), "Studied minutes cannot be negative.");

        var assessment = new StudyAreaWeekAssessment(id, weekIndividualGoal, studyAreaWeekId);
        assessment.MinutesStudied = minutesStudied;
        return assessment;
    }

    public void RecalculateGoal(decimal weekIndividualGoal)
    {
        if (weekIndividualGoal <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(weekIndividualGoal), "The individual weekly goal must be greater than zero."
            );

        WeekIndividualGoal = weekIndividualGoal;
    }

    public void UpdateMinutesStudied(int minutesStudied)
    {
        if (minutesStudied < 0)
            throw new ArgumentOutOfRangeException(nameof(minutesStudied), "Studied minutes cannot be negative.");

        MinutesStudied = minutesStudied;
    }
}