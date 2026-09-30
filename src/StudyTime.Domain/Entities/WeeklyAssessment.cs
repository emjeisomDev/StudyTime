using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class WeeklyAssessment
{
    public Guid Id { get; }
    public int WeekNumber { get; }
    public int Year { get; }
    public decimal WeekGlobalGoal { get; private set; }
    public int MinutesStudied { get; private set; }

    public WeeklyAssessment(
        int weekNumber,
        int year,
        decimal weekGlobalGoal,
        int minutesStudied = 0)
        : this(
            Guid.NewGuid(),
            weekNumber,
            year,
            weekGlobalGoal,
            minutesStudied)
    {
    }

    public WeeklyAssessment(
        Guid id,
        int weekNumber,
        int year,
        decimal weekGlobalGoal,
        int minutesStudied = 0)
    {
        if (id == Guid.Empty)
            throw new DomainException("WeeklyAssessment id cannot be empty.");

        if (weekNumber is < 1 or > 53)
            throw new DomainException("Week number must be between 1 and 53.");

        if (year <= 0)
            throw new DomainException("Year must be positive.");

        if (weekGlobalGoal <= 0)
            throw new DomainException("Weekly global goal must be positive.");

        if (minutesStudied < 0)
            throw new DomainException("Minutes studied cannot be negative.");

        Id = id;
        WeekNumber = weekNumber;
        Year = year;
        WeekGlobalGoal = weekGlobalGoal;
        MinutesStudied = minutesStudied;
    }

    public void UpdateGlobalGoal(decimal weekGlobalGoal)
    {
        if (weekGlobalGoal <= 0)
            throw new DomainException("Weekly global goal must be positive.");

        WeekGlobalGoal = weekGlobalGoal;
    }

    public void UpdateMinutesStudied(int minutesStudied)
    {
        if (minutesStudied < 0)
            throw new DomainException("Minutes studied cannot be negative.");

        MinutesStudied = minutesStudied;
    }
}