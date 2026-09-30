using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyRecordTests
{
    private static readonly DateOnly ValidDate = new(2026, 9, 28);
    private static readonly Guid ValidStudyAreaWeekId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidData_CreatesStudyRecord()
    {
        var minutes = new Minutes(30);

        var record = new StudyRecord(
            ValidDate,
            minutes,
            ValidStudyAreaWeekId);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(ValidDate, record.Date);
        Assert.Equal(minutes, record.Minutes);
        Assert.Equal(ValidStudyAreaWeekId, record.StudyAreaWeekId);
    }

    [Fact]
    public void Constructor_WithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();
        var minutes = new Minutes(45);

        var record = new StudyRecord(
            id,
            ValidDate,
            minutes,
            ValidStudyAreaWeekId);

        Assert.Equal(id, record.Id);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyRecord(
                Guid.Empty,
                ValidDate,
                new Minutes(30),
                ValidStudyAreaWeekId));
    }

    [Fact]
    public void Constructor_EmptyStudyAreaWeekId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyRecord(
                ValidDate,
                new Minutes(30),
                Guid.Empty));
    }

    [Fact]
    public void Constructor_NullMinutes_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyRecord(
                ValidDate,
                null!,
                ValidStudyAreaWeekId));
    }

    [Fact]
    public void Constructor_CreatedAt_IsNotSetByConstructor()
    {
        var record = new StudyRecord(
            ValidDate,
            new Minutes(30),
            ValidStudyAreaWeekId);

        Assert.Equal(default, record.CreatedAt);
    }

    [Fact]
    public void Constructor_SameDate_AllowsMultipleRecords()
    {
        var first = new StudyRecord(
            ValidDate,
            new Minutes(30),
            ValidStudyAreaWeekId);

        var second = new StudyRecord(
            ValidDate,
            new Minutes(45),
            ValidStudyAreaWeekId);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.Date, second.Date);
        Assert.Equal(first.StudyAreaWeekId, second.StudyAreaWeekId);
    }
}