using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyRecord
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int Minutes { get; private set; }
    public Guid StudyAreaWeekId { get; private set; }
    public StudyAreaWeek StudyAreaWeek { get; private set; } = null!;


    private StudyRecord() { }

    private StudyRecord(
        Guid id,
        DateOnly date,
        DateTimeOffset createdAt,
        StudyMinutes minutes,
        Guid studyAreaWeekId)
    {
        Id = id;
        Date = date;
        CreatedAt = createdAt;
        Minutes = minutes.Value;
        StudyAreaWeekId = studyAreaWeekId;
    }

    public static StudyRecord Create(
        DateOnly date,
        int minutes,
        Guid studyAreaWeekId,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            throw new ArgumentException(
                "The weekly configuration identifier is required.",
                nameof(studyAreaWeekId));
        }

        var studyMinutes = StudyMinutes.From(minutes);

        return new StudyRecord(
            id ?? Guid.NewGuid(),
            date,
            createdAt ?? DateTimeOffset.UtcNow,
            studyMinutes,
            studyAreaWeekId);
    }
}