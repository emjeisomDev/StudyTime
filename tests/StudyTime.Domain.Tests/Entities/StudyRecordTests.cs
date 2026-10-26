using Xunit;
using System.Reflection;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyRecordTests
{
    [Fact]
    public void Constructor_DadosValidos_AtribuiPropriedadesCorretamente()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        DateOnly date = new(2026, 8, 31);
        Minutes minutes = new(45);
        Guid studyAreaWeekId = Guid.NewGuid();

        // Act
        StudyRecord studyRecord = new(id, date, minutes, studyAreaWeekId);

        // Assert
        Assert.Equal(id, studyRecord.Id);
        Assert.Equal(date, studyRecord.Date);
        Assert.Equal(minutes, studyRecord.Minutes);
        Assert.Equal(studyAreaWeekId, studyRecord.StudyAreaWeekId);
    }

    [Fact]
    public void Constructor_IdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyRecord(Guid.Empty, new DateOnly(2026, 8, 31), new Minutes(45), Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyRecord id cannot be empty.", exception.Message);
    }

    [Fact]
    public void Constructor_MinutesNulo_LancaDomainException()
    {
        // Arrange
        Minutes? minutes = null;

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyRecord(Guid.NewGuid(), new DateOnly(2026, 8, 31), minutes!, Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyRecord minutes cannot be null.", exception.Message);
    }

    [Fact]
    public void Constructor_StudyAreaWeekIdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyRecord(Guid.NewGuid(), new DateOnly(2026, 8, 31), new Minutes(45), Guid.Empty);
            });

        // Assert
        Assert.Contains("StudyAreaWeekId cannot be empty.", exception.Message);
    }

    [Fact]
    public void Propriedades_SemSettersPublicos_SaoImutaveisExternamente()
    {
        // Arrange
        PropertyInfo[] properties = typeof(StudyRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Act
        // Assert
        Assert.All(properties, property => Assert.Null(property.GetSetMethod()));
    }
}
