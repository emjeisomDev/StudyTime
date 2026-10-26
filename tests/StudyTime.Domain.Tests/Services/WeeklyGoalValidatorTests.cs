using Xunit;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class WeeklyGoalValidatorTests
{
    [Theory]
    [InlineData(1500)]
    [InlineData(1800)]
    public void Validate_MetaGlobalNoMinimoOuAcima_NaoLancaExcecao(decimal globalGoal)
    {
        // Arrange
        // Act
        Exception? exception = Record.Exception(() => WeeklyGoalValidator.Validate(globalGoal));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(1499)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_MetaGlobalAbaixoDoMinimo_LancaWeeklyGoalNotMetException(
        decimal globalGoal)
    {
        // Arrange
        // Act
        WeeklyGoalNotMetException exception = Assert.Throws<WeeklyGoalNotMetException>(
            () => WeeklyGoalValidator.Validate(globalGoal));

        // Assert
        Assert.Contains("The weekly global goal must be at least 1500 minutes.", exception.Message);
        Assert.Contains($"Actual: {globalGoal}.", exception.Message);
    }

    [Fact]
    public void Validate_MetasIndividuaisComSomaDe1500_NaoLancaExcecao()
    {
        // Arrange
        decimal[] individualGoals = [600m, 500m, 400m];

        // Act
        Exception? exception = Record.Exception(
            () => WeeklyGoalValidator.Validate(individualGoals));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MetasIndividuaisComSomaAcimaDoMinimo_NaoLancaExcecao()
    {
        // Arrange
        decimal[] individualGoals = [800m, 800m];

        // Act
        Exception? exception = Record.Exception(
            () => WeeklyGoalValidator.Validate(individualGoals));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MetasIndividuaisComSomaAbaixoDoMinimo_LancaWeeklyGoalNotMetException()
    {
        // Arrange
        decimal[] individualGoals = [600m, 500m, 399m];

        // Act
        WeeklyGoalNotMetException exception = Assert.Throws<WeeklyGoalNotMetException>(
            () => WeeklyGoalValidator.Validate(individualGoals));

        // Assert
        Assert.Contains("The weekly global goal must be at least 1500 minutes.", exception.Message);
        Assert.Contains("Actual: 1499.", exception.Message);
    }

    [Fact]
    public void Validate_ListaDeMetasVazia_LancaWeeklyGoalNotMetException()
    {
        // Arrange
        decimal[] individualGoals = [];

        // Act
        WeeklyGoalNotMetException exception = Assert.Throws<WeeklyGoalNotMetException>(
            () => WeeklyGoalValidator.Validate(individualGoals));

        // Assert
        Assert.Contains("The weekly global goal must be at least 1500 minutes.", exception.Message);
        Assert.Contains("Actual: 0.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_MetaIndividualNaoPositiva_LancaArgumentOutOfRangeException(
        decimal invalidGoal)
    {
        // Arrange
        decimal[] individualGoals = [600m, invalidGoal];

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => WeeklyGoalValidator.Validate(individualGoals));

        // Assert
        Assert.Contains("Individual goals must be positive.", exception.Message);
    }

    [Fact]
    public void Validate_ListaDeMetasNula_LancaArgumentNullException()
    {
        // Arrange
        IEnumerable<decimal>? individualGoals = null;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => WeeklyGoalValidator.Validate(individualGoals!));

        // Assert
        Assert.Contains("individualGoals", exception.Message);
    }
}
