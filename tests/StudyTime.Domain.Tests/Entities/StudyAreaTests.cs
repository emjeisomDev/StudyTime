using Xunit;
using System.Reflection;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaTests
{
    [Fact]
    public void Constructor_DadosValidos_AtribuiPropriedadesCorretamente()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        const string name = "Matemática";
        Minutes standardWeeklyStudyTime = new(120);

        // Act
        StudyArea studyArea = new(id, name, standardWeeklyStudyTime);

        // Assert
        Assert.Equal(id, studyArea.Id);
        Assert.Equal(name, studyArea.Name);
        Assert.Equal(standardWeeklyStudyTime, studyArea.StdWeekStudyTime);
    }

    [Fact]
    public void Constructor_IdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyArea(Guid.Empty, "Matemática", new Minutes(120));
            });

        // Assert
        Assert.Contains("StudyArea id cannot be empty.", exception.Message);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "cannot be an empty string")]
    [InlineData("   ", "cannot be an empty string")]
    public void Constructor_NomeNuloVazioOuBranco_LancaArgumentException(
        string? name,
        string expectedMessage)
    {
        // Arrange
        // Act
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(
            () =>
            {
                _ = new StudyArea(Guid.NewGuid(), name!, new Minutes(120));
            });

        // Assert
        Assert.Contains(expectedMessage.ToLowerInvariant(), exception.Message.ToLowerInvariant());
    }

    [Fact]
    public void Constructor_NomeComMaisDe80Caracteres_LancaDomainException()
    {
        // Arrange
        string name = new('A', 81);

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyArea(Guid.NewGuid(), name, new Minutes(120));
            });

        // Assert
        Assert.Contains("StudyArea name cannot exceed 80 characters.", exception.Message);
    }

    [Fact]
    public void Constructor_MetaSemanalNula_LancaArgumentNullException()
    {
        // Arrange
        Minutes? standardWeeklyStudyTime = null;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () =>
            {
                _ = new StudyArea(Guid.NewGuid(), "Matemática", standardWeeklyStudyTime!);
            });

        // Assert
        Assert.Contains("Value cannot be null.", exception.Message);
        Assert.Contains("stdWeekStudyTime", exception.Message);
    }

    [Fact]
    public void Propriedades_SemSettersPublicos_SaoImutaveisExternamente()
    {
        // Arrange
        PropertyInfo[] properties = typeof(StudyArea).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Act
        // Assert
        Assert.All(properties, property => Assert.Null(property.GetSetMethod()));
    }
}