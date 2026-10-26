using Xunit;
using System.Reflection;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaWeekTests
{
    private static readonly DateOnly Monday = new(2026, 8, 31);

    [Fact]
    public void Constructor_DadosValidos_AtribuiPropriedadesCorretamente()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid studyAreaId = Guid.NewGuid();
        Guid studyPlanId = Guid.NewGuid();
        Guid weeklyAssessmentId = Guid.NewGuid();

        // Act
        StudyAreaWeek studyAreaWeek = new(id, Monday, studyAreaId, studyPlanId, weeklyAssessmentId);

        // Assert
        Assert.Equal(id, studyAreaWeek.Id);
        Assert.Equal(Monday, studyAreaWeek.WeekStartDate);
        Assert.Equal(studyAreaId, studyAreaWeek.StudyAreaId);
        Assert.Equal(studyPlanId, studyAreaWeek.StudyPlanId);
        Assert.Equal(weeklyAssessmentId, studyAreaWeek.WeeklyAssessmentId);
    }

    [Fact]
    public void Constructor_IdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeek(Guid.Empty, Monday, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyAreaWeek id cannot be empty.", exception.Message);
    }

    [Theory]
    [InlineData(2026, 9, 1)]
    [InlineData(2026, 9, 6)]
    public void Constructor_DataQueNaoEsegunda_LancaDomainException(int year, int month, int day)
    {
        // Arrange
        DateOnly date = new(year, month, day);

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeek(date, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyAreaWeek must start on a Monday.", exception.Message);
    }

    [Fact]
    public void Constructor_StudyAreaIdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeek(Monday, Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyAreaId cannot be empty.", exception.Message);
    }

    [Fact]
    public void Constructor_StudyPlanIdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeek(Monday, Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyPlanId cannot be empty.", exception.Message);
    }

    [Fact]
    public void Constructor_WeeklyAssessmentIdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeek(Monday, Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
            });

        // Assert
        Assert.Contains("WeeklyAssessmentId cannot be empty.", exception.Message);
    }

    [Fact]
    public void Propriedades_SemSettersPublicos_SaoImutaveisExternamente()
    {
        // Arrange
        PropertyInfo[] properties = typeof(StudyAreaWeek).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Act
        // Assert
        Assert.All(properties, property => Assert.Null(property.GetSetMethod()));
    }
}