using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaTests
{
    [Fact]
    public void Constructor_ValidData_CreatesStudyArea()
    {
        var id = Guid.NewGuid();
        var minutes = new Minutes(120);

        var studyArea = new StudyArea(id, "Mathematics", minutes);

        Assert.Equal(id, studyArea.Id);
        Assert.Equal("Mathematics", studyArea.Name);
        Assert.Equal(minutes, studyArea.StdWeekStudyTime);
    }

    [Fact]
    public void Constructor_WithoutId_GeneratesId()
    {
        var studyArea = new StudyArea(
            "Mathematics",
            new Minutes(120));

        Assert.NotEqual(Guid.Empty, studyArea.Id);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StudyArea(
                Guid.Empty,
                "Mathematics",
                new Minutes(120)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_EmptyName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new StudyArea(name, new Minutes(120)));
    }

    [Fact]
    public void Constructor_NameLongerThan80Characters_ThrowsDomainException()
    {
        var name = new string('A', 81);

        Assert.Throws<DomainException>(() =>
            new StudyArea(name, new Minutes(120)));
    }

    [Fact]
    public void Constructor_NameWith80Characters_CreatesStudyArea()
    {
        var name = new string('A', 80);

        var studyArea = new StudyArea(name, new Minutes(120));

        Assert.Equal(name, studyArea.Name);
    }

    [Fact]
    public void Constructor_NullMinutes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StudyArea("Mathematics", null!));
    }
}