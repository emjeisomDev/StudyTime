using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeekAssessment
{
    public Guid Id { get; init; }
    public decimal WeekIndividualGoal { get; private set; }
    public int MinutesStudied { get; private set; }
    public Guid StudyAreaWeekId { get; init; }


    public StudyAreaWeekAssessment(
        Guid id,
        decimal weekIndividualGoal,
        Guid studyAreaWeekId,
        int minutesStudied = 0)
    {
        ValidateGoal(weekIndividualGoal);
        ValidateMinutes(minutesStudied);

        Id = id;
        WeekIndividualGoal = weekIndividualGoal;
        StudyAreaWeekId = studyAreaWeekId;
        MinutesStudied = minutesStudied;
    }

    public void SetGoal(decimal weekIndividualGoal)
    {
        ValidateGoal(weekIndividualGoal);

        WeekIndividualGoal = weekIndividualGoal;
    }

    public void SetMinutes(int minutesStudied)
    {
        ValidateMinutes(minutesStudied);

        MinutesStudied = minutesStudied;
    }

    private static void ValidateGoal(decimal weekIndividualGoal)
    {
        if (weekIndividualGoal <= 0)
        {
            throw new DomainValidationException("Individual weekly goal must be greater than zero.");
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