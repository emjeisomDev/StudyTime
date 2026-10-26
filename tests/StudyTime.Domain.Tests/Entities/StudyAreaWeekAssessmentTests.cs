using Xunit;
using System.Reflection;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyAreaWeekAssessmentTests
{
    [Fact]
    public void Constructor_DadosValidos_AtribuiPropriedadesCorretamente()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        const decimal individualGoal = 180m;
        const int minutesStudied = 45;
        Guid studyAreaWeekId = Guid.NewGuid();

        // Act
        StudyAreaWeekAssessment assessment = new(id, individualGoal, minutesStudied, studyAreaWeekId);

        // Assert
        Assert.Equal(id, assessment.Id);
        Assert.Equal(individualGoal, assessment.WeekIndividualGoal);
        Assert.Equal(minutesStudied, assessment.MinutesStudied);
        Assert.Equal(studyAreaWeekId, assessment.StudyAreaWeekId);
    }

    [Fact]
    public void Constructor_IdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeekAssessment(Guid.Empty, 180m, 45, Guid.NewGuid());
            });

        // Assert
        Assert.Contains("StudyAreaWeekAssessment id cannot be empty.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_MetaIndividualNaoPositiva_LancaDomainException(decimal individualGoal)
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeekAssessment(Guid.NewGuid(), individualGoal, 0, Guid.NewGuid());
            });

        // Assert
        Assert.Contains("Weekly individual goal must be positive.", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public void Constructor_MinutosEstudadosNegativos_LancaDomainException(int minutesStudied)
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeekAssessment(Guid.NewGuid(), 180m, minutesStudied, Guid.NewGuid());
            });

        // Assert
        Assert.Contains("Minutes studied cannot be negative.", exception.Message);
    }

    [Fact]
    public void Constructor_StudyAreaWeekIdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyAreaWeekAssessment(Guid.NewGuid(), 180m, 45, Guid.Empty);
            });

        // Assert
        Assert.Contains("StudyAreaWeekId cannot be empty.", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(240)]
    public void UpdateIndividualGoal_MetaPositiva_AtualizaMeta(decimal individualGoal)
    {
        // Arrange
        StudyAreaWeekAssessment assessment = new(180m, Guid.NewGuid());

        // Act
        assessment.UpdateIndividualGoal(individualGoal);

        // Assert
        Assert.Equal(individualGoal, assessment.WeekIndividualGoal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateIndividualGoal_MetaNaoPositiva_LancaDomainException(decimal individualGoal)
    {
        // Arrange
        StudyAreaWeekAssessment assessment = new(180m, Guid.NewGuid());

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () => assessment.UpdateIndividualGoal(individualGoal));

        // Assert
        Assert.Contains("Weekly individual goal must be positive.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(120)]
    public void UpdateMinutesStudied_MinutosNaoNegativos_AtualizaTotal(int minutesStudied)
    {
        // Arrange
        StudyAreaWeekAssessment assessment = new(180m, Guid.NewGuid());

        // Act
        assessment.UpdateMinutesStudied(minutesStudied);

        // Assert
        Assert.Equal(minutesStudied, assessment.MinutesStudied);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public void UpdateMinutesStudied_MinutosNegativos_LancaDomainException(int minutesStudied)
    {
        // Arrange
        StudyAreaWeekAssessment assessment = new(180m, Guid.NewGuid());

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () => assessment.UpdateMinutesStudied(minutesStudied));

        // Assert
        Assert.Contains("Minutes studied cannot be negative.", exception.Message);
    }

    [Fact]
    public void Propriedades_SemSettersPublicos_SaoImutaveisExternamente()
    {
        // Arrange
        PropertyInfo[] properties = typeof(StudyAreaWeekAssessment).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Act
        // Assert
        Assert.All(properties, property => Assert.Null(property.GetSetMethod()));
    }
}
