using Xunit;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class GoalCalculatorTests
{
    [Theory]
    [InlineData(600, 2.5, 1500)]
    [InlineData(120, 1.5, 180)]
    [InlineData(1, 0.1, 0.1)]
    public void CalculateIndividualGoal_ValoresPositivos_RetornaMultiplicacaoEsperada(
        int standardWeeklyStudyTime,
        decimal coefficient,
        decimal expected)
    {
        // Arrange
        // Act
        decimal result = GoalCalculator.CalculateIndividualGoal(standardWeeklyStudyTime, coefficient);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateIndividualGoal_TempoPadraoNaoPositivo_LancaArgumentOutOfRangeException
        (int standardWeeklyStudyTime)
    {
        // Arrange
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => GoalCalculator.CalculateIndividualGoal(standardWeeklyStudyTime, 2.5m));

        // Assert
        Assert.Contains("Standard weekly study time must be positive.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    public void CalculateIndividualGoal_CoeficienteNaoPositivo_LancaArgumentOutOfRangeException
        (decimal coefficient)
    {
        // Arrange
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => GoalCalculator.CalculateIndividualGoal(600, coefficient));

        // Assert
        Assert.Contains("Coefficient must be positive.", exception.Message);
    }

    [Fact]
    public void CalculateGlobalGoal_MetasPositivas_RetornaSoma()
    {
        // Arrange
        decimal[] individualGoals = [600m, 500m, 400m];

        // Act
        decimal result = GoalCalculator.CalculateGlobalGoal(individualGoals);

        // Assert
        Assert.Equal(1500m, result);
    }

    [Fact]
    public void CalculateGlobalGoal_ListaVazia_RetornaZero()
    {
        // Arrange
        decimal[] individualGoals = [];

        // Act
        decimal result = GoalCalculator.CalculateGlobalGoal(individualGoals);

        // Assert
        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateGlobalGoal_MetaIndividualNaoPositiva_LancaArgumentOutOfRangeException
        (decimal invalidGoal)
    {
        // Arrange
        decimal[] individualGoals = [600m, invalidGoal];

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => GoalCalculator.CalculateGlobalGoal(individualGoals));

        // Assert
        Assert.Contains("Individual goals must be positive.", exception.Message);
    }

    [Fact]
    public void CalculateGlobalGoal_ListaNula_LancaArgumentNullException()
    {
        // Arrange
        IEnumerable<decimal>? individualGoals = null;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => GoalCalculator.CalculateGlobalGoal(individualGoals!));

        // Assert
        Assert.Contains("individualGoals", exception.Message);
    }
}
