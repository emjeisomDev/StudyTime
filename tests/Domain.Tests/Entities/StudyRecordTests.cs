using StudyTime.Domain.Entities;

namespace Domain.Tests.Entities;

public sealed class StudyRecordTests
{
    private static readonly DateOnly WeekStart = new(2026, 8, 31);

    [Fact]
    public void CreateShouldGenerateValidStudyRecord()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 2);

        var record = StudyRecord.Create(date, 60, studyAreaWeekId, WeekStart);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(date, record.Date);
        Assert.Equal(60, record.Minutes);
        Assert.Equal(studyAreaWeekId, record.StudyAreaWeekId);
        Assert.NotEqual(default, record.CreatedAt);
    }

    [Fact]
    public void CreateShouldUseUtcCreatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 9, 2, 15, 30, 0, TimeSpan.FromHours(-3));

        var record = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 2), createdAt, 60, Guid.NewGuid(), WeekStart);

        Assert.Equal(TimeSpan.Zero, record.CreatedAt.Offset);
        Assert.Equal(createdAt.ToUniversalTime(), record.CreatedAt);
    }

    [Fact]
    public void CreateWithExplicitIdShouldPreserveId()
    {
        var id = Guid.NewGuid();

        var record = StudyRecord.Create(id, new DateOnly(2026, 9, 2), DateTimeOffset.UtcNow, 60, Guid.NewGuid(), WeekStart);

        Assert.Equal(id, record.Id);
    }

    [Fact]
    public void CreateShouldRejectEmptyId()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyRecord.Create(Guid.Empty, new DateOnly(2026, 9, 2), DateTimeOffset.UtcNow, 60, Guid.NewGuid(), WeekStart));
    }

    [Fact]
    public void CreateShouldRejectNonPositiveMinutes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 2), 0, Guid.NewGuid(), WeekStart));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 2), -1, Guid.NewGuid(), WeekStart));
    }

    [Fact]
    public void CreateShouldRejectEmptyStudyAreaWeekId()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 2), 60, Guid.Empty, WeekStart));
    }

    [Fact]
    public void CreateShouldAcceptMondayOfConfiguredWeek()
    {
        var record = StudyRecord.Create(WeekStart, 60, Guid.NewGuid(), WeekStart);

        Assert.Equal(WeekStart, record.Date);
    }

    [Fact]
    public void CreateShouldAcceptSundayOfConfiguredWeek()
    {
        var sunday = WeekStart.AddDays(6);

        var record = StudyRecord.Create(sunday, 60, Guid.NewGuid(), WeekStart);

        Assert.Equal(sunday, record.Date);
    }

    [Fact]
    public void CreateShouldRejectDateBeforeConfiguredWeek()
    {
        var date = WeekStart.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(date, 60, Guid.NewGuid(), WeekStart));
    }

    [Fact]
    public void CreateShouldRejectDateAfterConfiguredWeek()
    {
        var date = WeekStart.AddDays(7);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StudyRecord.Create(date, 60, Guid.NewGuid(), WeekStart));
    }

    [Fact]
    public void CreateShouldRejectNonMondayWeekStartDate()
    {
        var invalidWeekStart = new DateOnly(2026, 9, 1);

        Assert.Throws<ArgumentException>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 2), 60, Guid.NewGuid(), invalidWeekStart));
    }

    [Fact]
    public void SelectLastForDeletionShouldReturnNewestRecord()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var older = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow.AddMinutes(-10), 30, studyAreaWeekId, WeekStart);
        var newer = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow, 60, studyAreaWeekId, WeekStart);

        var records = new[] { older, newer };

        var result = StudyRecord.SelectLastForDeletion(records);

        Assert.Equal(newer.Id, result.Id);
    }

    [Fact]
    public void SelectLastForDeletionShouldUseIdAsTieBreaker()
    {
        var createdAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var lowerId = new Guid("00000000-0000-0000-0000-000000000001");
        var higherId = new Guid("00000000-0000-0000-0000-000000000002");
        var studyAreaWeekId = Guid.NewGuid();

        var first = StudyRecord.Create(lowerId, new DateOnly(2026, 9, 1), createdAt, 30, studyAreaWeekId, WeekStart);
        var second = StudyRecord.Create(higherId, new DateOnly(2026, 9, 1), createdAt, 60, studyAreaWeekId, WeekStart);

        var records = new[] { first, second };

        var result = StudyRecord.SelectLastForDeletion(records);

        Assert.Equal(higherId, result.Id);
    }

    [Fact]
    public void SelectLastForDeletionShouldWorkRegardlessOfInputOrder()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var older = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow.AddHours(-1), 30, studyAreaWeekId, WeekStart);
        var newer = StudyRecord.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow, 60, studyAreaWeekId, WeekStart);

        var records = new[] { newer, older };

        var result = StudyRecord.SelectLastForDeletion(records);

        Assert.Equal(newer.Id, result.Id);
    }

    [Fact]
    public void SelectLastForDeletionShouldRejectNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StudyRecord.SelectLastForDeletion(null!));
    }

    [Fact]
    public void SelectLastForDeletionShouldRejectEmptyCollection()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StudyRecord.SelectLastForDeletion(Array.Empty<StudyRecord>()));
    }
}