namespace StudyTime.Domain.Entities;

public sealed class WeeklyAssessment
{
    public Guid Id { get; private set; }
    public int WeekNumber { get; private set; }
    public int Year { get; private set; }
    public decimal WeekGlobalGoal { get; private set; }
    public int MinutesStudied { get; private set; }

    private WeeklyAssessment()
    {
    }

    private WeeklyAssessment(Guid id, int year, int weekNumber, decimal weekGlobalGoal, int minutesStudied)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The weekly assessment id must not be empty.", nameof(id));
        ValidateIsoWeek(year, weekNumber);
        if (weekGlobalGoal <= 0)
            throw new ArgumentOutOfRangeException(nameof(weekGlobalGoal), "The global weekly goal must be greater than zero.");
        if (minutesStudied < 0)
            throw new ArgumentOutOfRangeException(nameof(minutesStudied), "Studied minutes cannot be negative.");

        Id = id;
        Year = year;
        WeekNumber = weekNumber;
        WeekGlobalGoal = weekGlobalGoal;
        MinutesStudied = minutesStudied;
    }

    public static WeeklyAssessment Create(int year, int weekNumber, decimal weekGlobalGoal)
        => new(Guid.NewGuid(), year, weekNumber, weekGlobalGoal, 0);

    public static WeeklyAssessment Create(Guid id, int year, int weekNumber, decimal weekGlobalGoal, int minutesStudied = 0)
        => new(id, year, weekNumber, weekGlobalGoal, minutesStudied);

    public void UpdateGlobalGoal(decimal weekGlobalGoal)
    {
        if (weekGlobalGoal <= 0)
            throw new ArgumentOutOfRangeException(nameof(weekGlobalGoal), "The global weekly goal must be greater than zero.");

        WeekGlobalGoal = weekGlobalGoal;
    }

    public void UpdateMinutesStudied(int minutesStudied)
    {
        if (minutesStudied < 0)
            throw new ArgumentOutOfRangeException(nameof(minutesStudied), "Studied minutes cannot be negative.");

        MinutesStudied = minutesStudied;
    }

    public bool IsGoalAchieved(IEnumerable<StudyAreaWeekAssessment> assessments)
    {
        ArgumentNullException.ThrowIfNull(assessments);

        var materialized = assessments.ToArray();
        return WeekGlobalGoal > 0 && materialized.Length > 0 && materialized.All(assessment => assessment.GoalAchieved);
    }

    public static decimal CalculateGlobalGoal(IEnumerable<StudyAreaWeekAssessment> assessments)
    {
        ArgumentNullException.ThrowIfNull(assessments);

        var materialized = assessments.ToArray();
        if (materialized.Length == 0)
            throw new ArgumentException("At least one individual assessment is required.", nameof(assessments));

        var total = materialized.Sum(assessment => assessment.WeekIndividualGoal);
        if (total <= 0)
            throw new InvalidOperationException("The global weekly goal must be greater than zero.");

        return total;
    }

    public void RecalculateGlobalGoal(IEnumerable<StudyAreaWeekAssessment> assessments)
    {
        UpdateGlobalGoal(CalculateGlobalGoal(assessments));
    }

    private static void ValidateIsoWeek(int year, int weekNumber)
    {
        if (year <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), "The year must be greater than zero.");
        if (weekNumber is < 1 or > 53)
            throw new ArgumentOutOfRangeException(nameof(weekNumber), "The ISO week number must be between 1 and 53.");
    }
}