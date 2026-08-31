namespace StudyTime.Domain.Entities;

public sealed class StudyRecord
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int Minutes { get; private set; }
    public Guid StudyAreaWeekId { get; private set; }

    private StudyRecord()
    {
    }

    private StudyRecord(Guid id, DateOnly date, DateTimeOffset createdAt, int minutes, Guid studyAreaWeekId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The study record id must not be empty.", nameof(id));
        if (minutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(minutes), "Study minutes must be greater than zero.");
        if (studyAreaWeekId == Guid.Empty)
            throw new ArgumentException("The study area week id must not be empty.", nameof(studyAreaWeekId));

        Id = id;
        Date = date;
        CreatedAt = createdAt.ToUniversalTime();
        Minutes = minutes;
        StudyAreaWeekId = studyAreaWeekId;
    }

    public static StudyRecord Create(DateOnly date, int minutes, Guid studyAreaWeekId, DateOnly weekStartDate)
    {
        return Create(Guid.NewGuid(), date, DateTimeOffset.UtcNow, minutes, studyAreaWeekId, weekStartDate);
    }

    public static StudyRecord Create(Guid id, DateOnly date, DateTimeOffset createdAt, int minutes, Guid studyAreaWeekId, DateOnly weekStartDate)
    {
        ValidateDateBelongsToWeek(date, weekStartDate);
        return new StudyRecord(id, date, createdAt, minutes, studyAreaWeekId);
    }

    public static StudyRecord SelectLastForDeletion(IEnumerable<StudyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var last = records
            .OrderByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id)
            .FirstOrDefault();

        return last ?? throw new InvalidOperationException("No study record is eligible for deletion.");
    }

    private static void ValidateDateBelongsToWeek(DateOnly date, DateOnly weekStartDate)
    {
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("The week start date must be a Monday.", nameof(weekStartDate));

        var weekEndDate = weekStartDate.AddDays(6);
        if (date < weekStartDate || date > weekEndDate)
            throw new ArgumentOutOfRangeException(
                nameof(date), "The study record date must belong to the configured week.");
    }
}