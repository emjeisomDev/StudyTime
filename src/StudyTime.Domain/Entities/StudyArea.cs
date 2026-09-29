
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyArea
{
    public Guid Id { get; }
    public string Name { get; }
    public Minutes StdWeekStudyTime { get; }

    public StudyArea(string name, Minutes stdWeekStudyTime)
        : this(Guid.NewGuid(), name, stdWeekStudyTime)
    {
    }

    public StudyArea(Guid id, string name, Minutes stdWeekStudyTime)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("StudyArea id cannot be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 80)
        {
            throw new DomainException("StudyArea name cannot exceed 80 characters.");
        }

        ArgumentNullException.ThrowIfNull(stdWeekStudyTime);

        Id = id;
        Name = name;
        StdWeekStudyTime = stdWeekStudyTime;
    }
}