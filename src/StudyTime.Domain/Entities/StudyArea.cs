using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyArea
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public int StdWeekStudyTime { get; private set; }

    private StudyArea() { }

    private StudyArea(Guid id, string name, StdWeekMinutes stdWeekStudyTime)
    {
        Id = id;
        Name = name;
        StdWeekStudyTime = stdWeekStudyTime.Value;
    }

    public static StudyArea Create(string name, int stdWeekStudyTime, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var standardMinutes = StdWeekMinutes.From(stdWeekStudyTime);

        return new StudyArea(id ?? Guid.NewGuid(), name.Trim(), standardMinutes);
    }
}