using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyRecord
{
    public Guid Id { get; }
    public DateOnly Date { get; }
    public DateTime CreatedAt { get; private set; }
    public Minutes Minutes { get; }
    public Guid StudyAreaWeekId { get; }

    public StudyRecord(
        DateOnly date,
        Minutes minutes,
        Guid studyAreaWeekId)
        : this(Guid.NewGuid(), date, minutes, studyAreaWeekId)
    {
    }

    public StudyRecord(
        Guid id,
        DateOnly date,
        Minutes minutes,
        Guid studyAreaWeekId)
    {
        if (id == Guid.Empty)
            throw new DomainException("StudyRecord id cannot be empty.");

        if (minutes is null)
            throw new DomainException("StudyRecord minutes cannot be null.");

        if (studyAreaWeekId == Guid.Empty)
            throw new DomainException("StudyAreaWeekId cannot be empty.");

        Id = id;
        Date = date;
        Minutes = minutes;
        StudyAreaWeekId = studyAreaWeekId;
    }
}