using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class StudyArea
{
    public Guid Id { get; init; }

    public string Name { get; init; }

    public int StdWeekStudyTime { get; init; }

    public StudyArea(Guid id, string name, int stdWeekStudyTime)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Study area name cannot be null, empty, or whitespace.",
                nameof(name));
        }

        if (name.Length > 80)
        {
            throw new ArgumentException(
                "Study area name cannot exceed 80 characters.",
                nameof(name));
        }

        if (stdWeekStudyTime <= 0)
        {
            throw new DomainValidationException("Standard weekly study time must be greater than zero.");
        }

        Id = id;
        Name = name;
        StdWeekStudyTime = stdWeekStudyTime;
    }

    public override string ToString()
    {
        return $"{Name} ({StdWeekStudyTime} minutes/week)";
    }
}