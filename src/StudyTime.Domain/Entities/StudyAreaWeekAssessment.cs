using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class StudyAreaWeekAssessment
{
    public Guid Id { get; }
    public decimal WeekIndividualGoal { get; private set; }
    public int MinutesStudied { get; private set; }
    public Guid StudyAreaWeekId { get; }

    public StudyAreaWeekAssessment(
        decimal weekIndividualGoal,
        Guid studyAreaWeekId,
        int minutesStudied = 0)
        : this(
            Guid.NewGuid(),
            weekIndividualGoal,
            minutesStudied,
            studyAreaWeekId)
    {
    }

    public StudyAreaWeekAssessment(
        Guid id,
        decimal weekIndividualGoal,
        int minutesStudied,
        Guid studyAreaWeekId)
    {
        if (id == Guid.Empty)
            throw new DomainException("StudyAreaWeekAssessment id cannot be empty.");

        if (weekIndividualGoal <= 0)
            throw new DomainException("Weekly individual goal must be positive.");

        if (minutesStudied < 0)
            throw new DomainException("Minutes studied cannot be negative.");

        if (studyAreaWeekId == Guid.Empty)
            throw new DomainException("StudyAreaWeekId cannot be empty.");

        Id = id;
        WeekIndividualGoal = weekIndividualGoal;
        MinutesStudied = minutesStudied;
        StudyAreaWeekId = studyAreaWeekId;
    }

    public void UpdateIndividualGoal(decimal weekIndividualGoal)
    {
        if (weekIndividualGoal <= 0)
            throw new DomainException("Weekly individual goal must be positive.");

        WeekIndividualGoal = weekIndividualGoal;
    }

    public void UpdateMinutesStudied(int minutesStudied)
    {
        if (minutesStudied < 0)
            throw new DomainException("Minutes studied cannot be negative.");

        MinutesStudied = minutesStudied;
    }
}