using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class WeeklyAssessment
{

    public Guid Id { get; private set; }
    public int WeekNumber { get; private set; }
    public int Year { get; private set; }
    public decimal WeekGlobalGoal { get; private set; }
    public int MinutesStudied { get; private set; }

    private WeeklyAssessment() { }

    private WeeklyAssessment(
        Guid id,
        int weekNumber,
        int year,
        decimal weekGlobalGoal,
        int minutesStudied)
    {
        Id = id;
        WeekNumber = weekNumber;
        Year = year;
        WeekGlobalGoal = weekGlobalGoal;
        MinutesStudied = minutesStudied;
    }

    public static WeeklyAssessment Create(
        int weekNumber,
        int year,
        decimal weekGlobalGoal,
        int minutesStudied = 0,
        Guid? id = null)
    {
        _ = IsoYearWeek.From(year, weekNumber);

        if (weekGlobalGoal <= 0)
        {
            throw new ArgumentOutOfRangeException
                (nameof(weekGlobalGoal), "The weekly global goal must be greater than zero.");
        }

        if (minutesStudied < 0)
        {
            throw new ArgumentOutOfRangeException
                (nameof(minutesStudied), "The minutes studied cannot be negative.");
        }

        return new WeeklyAssessment(
            id ?? Guid.NewGuid(),
            weekNumber,
            year,
            weekGlobalGoal,
            minutesStudied);
    }
}