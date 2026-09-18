using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class StudyRecord
{
    public Guid Id { get; init; }

    public DateOnly Date { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public int Minutes { get; init; }

    public Guid StudyAreaWeekId { get; init; }

    public StudyRecord(
        Guid id,
        DateOnly date,
        DateTimeOffset createdAt,
        int minutes,
        Guid studyAreaWeekId)
    {
        if (minutes <= 0)
        {
            throw new DomainValidationException("Study record minutes must be greater than zero.");
        }

        if (studyAreaWeekId == Guid.Empty)
        {
            throw new DomainValidationException("Study record must reference a valid study area week.");
        }

        Id = id;
        Date = date;
        CreatedAt = createdAt;
        Minutes = minutes;
        StudyAreaWeekId = studyAreaWeekId;
    }
}