namespace StudyTime.Domain.Entities;

public sealed class StudyArea
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int StdWeekStudyTime { get; private set; }

    private StudyArea()
    {
        Name = string.Empty;
    }

    private StudyArea(Guid id, string name, int stdWeekStudyTime)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The study area id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The study area name is required.", nameof(name));
        if (name.Length > 80)
            throw new ArgumentException("The study area name must contain at most 80 characters.", nameof(name));
        if (stdWeekStudyTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(stdWeekStudyTime), "The standard weekly study time must be greater than zero.");

        Id = id;
        Name = name.Trim();
        StdWeekStudyTime = stdWeekStudyTime;
    }

    public static StudyArea Create(string name, int stdWeekStudyTime)
        => new(Guid.NewGuid(), name, stdWeekStudyTime);

    public static StudyArea Create(Guid id, string name, int stdWeekStudyTime)
        => new(id, name, stdWeekStudyTime);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The study area name is required.", nameof(name));
        if (name.Length > 80)
            throw new ArgumentException("The study area name must contain at most 80 characters.", nameof(name));

        Name = name.Trim();
    }

    public void ChangeStandardWeeklyStudyTime(int stdWeekStudyTime)
    {
        if (stdWeekStudyTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(stdWeekStudyTime), "The standard weekly study time must be greater than zero.");

        StdWeekStudyTime = stdWeekStudyTime;
    }
}