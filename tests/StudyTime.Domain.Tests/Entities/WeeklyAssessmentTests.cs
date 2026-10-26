using Xunit;
using System.Reflection;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Tests.Entities;

public sealed class WeeklyAssessmentTests
{
    [Fact]
    public void Constructor_DadosValidos_AtribuiPropriedadesCorretamente()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        const int weekNumber = 42;
        const int year = 2026;
        const decimal globalGoal = 1500m;
        const int minutesStudied = 390;

        // Act
        WeeklyAssessment assessment = new(id, weekNumber, year, globalGoal, minutesStudied);

        // Assert
        Assert.Equal(id, assessment.Id);
        Assert.Equal(weekNumber, assessment.WeekNumber);
        Assert.Equal(year, assessment.Year);
        Assert.Equal(globalGoal, assessment.WeekGlobalGoal);
        Assert.Equal(minutesStudied, assessment.MinutesStudied);
    }

    [Fact]
    public void Constructor_IdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new WeeklyAssessment(Guid.Empty, 42, 2026, 1500m);
            });

        // Assert
        Assert.Contains("WeeklyAssessment id cannot be empty.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(54)]
    [InlineData(100)]
    public void Constructor_NumeroSemanaForaDoIntervalo_LancaDomainException(int weekNumber)
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new WeeklyAssessment(Guid.NewGuid(), weekNumber, 2026, 1500m);
            });

        // Assert
        Assert.Contains("Week number must be between 1 and 53.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_AnoNaoPositivo_LancaDomainException(int year)
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new WeeklyAssessment(Guid.NewGuid(), 1, year, 1500m);
            });

        // Assert
        Assert.Contains("Year must be positive.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_MetaGlobalNaoPositiva_LancaDomainException(decimal globalGoal)
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new WeeklyAssessment(Guid.NewGuid(), 1, 2026, globalGoal);
            });

        // Assert
        Assert.Contains("Weekly global goal must be positive.", exception.Message);
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
                _ = new WeeklyAssessment(Guid.NewGuid(), 1, 2026, 1500m, minutesStudied);
            });

        // Assert
        Assert.Contains("Minutes studied cannot be negative.", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(53)]
    public void Constructor_NumeroSemanaNosLimites_InstanciaCriada(int weekNumber)
    {
        // Arrange
        // Act
        WeeklyAssessment assessment = new(weekNumber, 2026, 1500m);

        // Assert
        Assert.Equal(weekNumber, assessment.WeekNumber);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3000)]
    public void UpdateGlobalGoal_MetaPositiva_AtualizaMeta(decimal globalGoal)
    {
        // Arrange
        WeeklyAssessment assessment = new(42, 2026, 1500m);

        // Act
        assessment.UpdateGlobalGoal(globalGoal);

        // Assert
        Assert.Equal(globalGoal, assessment.WeekGlobalGoal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateGlobalGoal_MetaNaoPositiva_LancaDomainException(decimal globalGoal)
    {
        // Arrange
        WeeklyAssessment assessment = new(42, 2026, 1500m);

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () => assessment.UpdateGlobalGoal(globalGoal));

        // Assert
        Assert.Contains("Weekly global goal must be positive.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1200)]
    public void UpdateMinutesStudied_MinutosNaoNegativos_AtualizaTotal(int minutesStudied)
    {
        // Arrange
        WeeklyAssessment assessment = new(42, 2026, 1500m);

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
        WeeklyAssessment assessment = new(42, 2026, 1500m);

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
        PropertyInfo[] properties = typeof(WeeklyAssessment).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Act
        // Assert
        Assert.All(properties, property => Assert.Null(property.GetSetMethod()));
    }
}
