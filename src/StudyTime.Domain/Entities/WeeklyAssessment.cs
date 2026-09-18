using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class WeeklyAssessment
{
    public Guid Id { get; init; }
    public int WeekNumber { get; init; }
    public int Year { get; init; }
    public decimal WeekGlobalGoal { get; private set; }
    public int MinutesStudied { get; private set; }


    public WeeklyAssessment(Guid id, int weekNumber, int year, decimal weekGlobalGoal, int minutesStudied = 0)
    {
        ValidateWeekNumber(weekNumber);
        ValidateYear(year);
        ValidateGoal(weekGlobalGoal);
        ValidateMinutes(minutesStudied);

        Id = id;
        WeekNumber = weekNumber;
        Year = year;
        WeekGlobalGoal = weekGlobalGoal;
        MinutesStudied = minutesStudied;
    }

    public void UpdateGoals(decimal weekGlobalGoal)
    {
        ValidateGoal(weekGlobalGoal);
        WeekGlobalGoal = weekGlobalGoal;
    }

    public void UpdateMinutes(int minutesStudied)
    {
        ValidateMinutes(minutesStudied);
        MinutesStudied = minutesStudied;
    }

    private static void ValidateWeekNumber(int weekNumber)
    {
        if (weekNumber is < 1 or > 53)
        {
            throw new DomainValidationException("Week number must be between 1 and 53.");
        }
    }

    private static void ValidateYear(int year)
    {
        if (year <= 0)
        {
            throw new DomainValidationException("Year must be greater than zero.");
        }
    }

    private static void ValidateGoal(decimal weekGlobalGoal)
    {
        if (weekGlobalGoal <= 0)
        {
            throw new DomainValidationException("Global weekly goal must be greater than zero.");
        }
    }

    private static void ValidateMinutes(int minutesStudied)
    {
        if (minutesStudied < 0)
        {
            throw new DomainValidationException("Studied minutes cannot be negative.");
        }
    }
}