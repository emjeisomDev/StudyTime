using StudyTime.Domain.Entities;

namespace Domain.Tests.Entities;

public sealed class StudyAreaTests
{
    [Fact]
    public void CreateShouldGenerateValidStudyArea()
    {
        var area = StudyArea.Create("C#", 600);

        Assert.NotEqual(Guid.Empty, area.Id);
        Assert.Equal("C#", area.Name);
        Assert.Equal(600, area.StdWeekStudyTime);
    }

    [Fact]
    public void CreateShouldTrimName()
    {
        var area = StudyArea.Create("  C#  ", 600);

        Assert.Equal("C#", area.Name);
    }

    [Fact]
    public void CreateWithExplicitIdShouldPreserveId()
    {
        var id = Guid.NewGuid();

        var area = StudyArea.Create(id, "C#", 600);

        Assert.Equal(id, area.Id);
    }

    [Fact]
    public void CreateShouldRejectEmptyId()
    {
        Assert.Throws<ArgumentException>(() => StudyArea.Create(Guid.Empty, "C#", 600));
    }

    [Fact]
    public void CreateShouldRejectNullName()
    {
        Assert.Throws<ArgumentException>(() => StudyArea.Create(null!, 600));
    }

    [Fact]
    public void CreateShouldRejectWhitespaceName()
    {
        Assert.Throws<ArgumentException>(() => StudyArea.Create("   ", 600));
    }

    [Fact]
    public void CreateShouldRejectNameLongerThan80Characters()
    {
        var name = new string('A', 81);

        Assert.Throws<ArgumentException>(() => StudyArea.Create(name, 600));
    }

    [Fact]
    public void CreateShouldRejectNonPositiveStandardWeeklyStudyTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyArea.Create("C#", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => StudyArea.Create("C#", -1));
    }

    [Fact]
    public void RenameShouldChangeName()
    {
        var area = StudyArea.Create("C#", 600);

        area.Rename("Mathematics");

        Assert.Equal("Mathematics", area.Name);
    }

    [Fact]
    public void RenameShouldTrimName()
    {
        var area = StudyArea.Create("C#", 600);

        area.Rename("  Mathematics  ");

        Assert.Equal("Mathematics", area.Name);
    }

    [Fact]
    public void RenameShouldRejectInvalidName()
    {
        var area = StudyArea.Create("C#", 600);

        Assert.Throws<ArgumentException>(() => area.Rename(""));
        Assert.Throws<ArgumentException>(() => area.Rename(new string('A', 81)));
    }

    [Fact]
    public void ChangeStandardWeeklyStudyTimeShouldUpdateValue()
    {
        var area = StudyArea.Create("C#", 600);

        area.ChangeStandardWeeklyStudyTime(900);

        Assert.Equal(900, area.StdWeekStudyTime);
    }

    [Fact]
    public void ChangeStandardWeeklyStudyTimeShouldRejectNonPositiveValue()
    {
        var area = StudyArea.Create("C#", 600);

        Assert.Throws<ArgumentOutOfRangeException>(() => area.ChangeStandardWeeklyStudyTime(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => area.ChangeStandardWeeklyStudyTime(-10));
    }
}