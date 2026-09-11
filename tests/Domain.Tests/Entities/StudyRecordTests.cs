using Xunit;
using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyRecordTests
{
    private static readonly Guid WeekId = Guid.NewGuid();

    [Fact]
    [Trait("Rule", "R05")]
    public void Create_ZeroMinutes_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 7), 0, WeekId));
    }

    [Fact]
    [Trait("Rule", "R05")]
    public void Create_NegativeMinutes_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 7), -1, WeekId));
    }

    [Fact]
    public void Create_ValidData_PreservesMinutes()
    {
        var record = StudyRecord.Create(new DateOnly(2026, 9, 7), 60, WeekId);
        Assert.Equal(60, record.Minutes);
    }

    [Fact]
    public void Create_ValidData_PreservesDate()
    {
        var date = new DateOnly(2026, 9, 7);
        var record = StudyRecord.Create(date, 60, WeekId);
        Assert.Equal(date, record.Date);
    }

    [Fact]
    public void Create_EmptyWeekId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StudyRecord.Create(new DateOnly(2026, 9, 7), 60, Guid.Empty));
    }

    [Fact]
    public void Create_ExplicitId_PreservesId()
    {
        var id = Guid.NewGuid();

        var record = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            60,
            WeekId,
            new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero),
            id);

        Assert.Equal(id, record.Id);
    }

    [Fact]
    public void Create_ExplicitCreatedAt_PreservesCreatedAt()
    {
        var createdAt = new DateTimeOffset(
            2026,
            9,
            7,
            10,
            30,
            0,
            TimeSpan.Zero);

        var record = StudyRecord.Create(
            new DateOnly(2026, 9, 7),
            60,
            WeekId,
            createdAt);

        Assert.Equal(createdAt, record.CreatedAt);
    }
}