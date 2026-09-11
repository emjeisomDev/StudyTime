using Xunit;
using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaTests
{
    [Fact]
    [Trait("Rule", "R03")]
    public void Create_StdWeekStudyTimeZero_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyArea.Create("Matemática", 0));
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void Create_StdWeekStudyTimeNegative_ThrowsDomainRuleViolationException()
    {
        Assert.ThrowsAny<Exception>(() => StudyArea.Create("Matemática", -1));
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void Create_ValidData_ReturnsStudyAreaWithTrimmedName()
    {
        var id = Guid.NewGuid();
        var area = StudyArea.Create("  Matemática  ", 120, id);
        Assert.Equal("Matemática", area.Name);
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void Create_ValidData_PreservesIdentifier()
    {
        var id = Guid.NewGuid();
        var area = StudyArea.Create("Matemática", 120, id);
        Assert.Equal(id, area.Id);
    }

    [Fact]
    [Trait("Rule", "R03")]
    public void Create_ValidData_PreservesStandardMinutes()
    {
        var area = StudyArea.Create("Matemática", 120);
        Assert.Equal(120, area.StdWeekStudyTime);
    }

    [Fact]
    public void Create_BlankName_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => StudyArea.Create("   ", 120));
    }
}