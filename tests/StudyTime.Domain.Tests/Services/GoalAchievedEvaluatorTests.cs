using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class GoalAchievedEvaluatorTests
{
    [Theory]
    [InlineData(1500, 1500, true)]
    [InlineData(1500, 1600, true)]
    [InlineData(1500, 1499, false)]
    [InlineData(1, 0, false)]
    public void EvaluateIndividual_MinutosEstudados_RetornaSeMetaFoiAtingida(
        decimal individualGoal,
        int minutesStudied,
        bool expected)
    {
        // Arrange
        StudyAreaWeekAssessment assessment = new(individualGoal, Guid.NewGuid(), minutesStudied);

        // Act
        bool result = GoalAchievedEvaluator.EvaluateIndividual(assessment);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EvaluateIndividual_AvaliacaoNula_LancaArgumentNullException()
    {
        // Arrange
        StudyAreaWeekAssessment? assessment = null;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => GoalAchievedEvaluator.EvaluateIndividual(assessment!));

        // Assert
        Assert.Contains("assessment", exception.Message);
    }

    [Theory]
    [InlineData("TodasAtingidas", true)]
    [InlineData("UmaNaoAtingida", false)]
    [InlineData("ListaVazia", false)]
    [InlineData("UnicaAtingida", true)]
    public void EvaluateGlobal_CenariosDeMetas_RetornaResultadoEsperado(
        string scenario,
        bool expected)
    {
        // Arrange
        StudyAreaWeekAssessment[] assessments = scenario switch
        {
            "TodasAtingidas" =>
            [
                CreateAssessment(600m, 600),
                CreateAssessment(900m, 1000)
            ],
            "UmaNaoAtingida" =>
            [
                CreateAssessment(600m, 600),
                CreateAssessment(900m, 899)
            ],
            "ListaVazia" => [],
            "UnicaAtingida" => [CreateAssessment(1500m, 1500)],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown test scenario.")
        };

        // Act
        bool result = GoalAchievedEvaluator.EvaluateGlobal(assessments);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EvaluateGlobal_ListaNula_LancaArgumentNullException()
    {
        // Arrange
        IEnumerable<StudyAreaWeekAssessment>? assessments = null;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => GoalAchievedEvaluator.EvaluateGlobal(assessments!));

        // Assert
        Assert.Contains("assessments", exception.Message);
    }

    [Fact]
    public void EvaluateGlobal_ListaContemAvaliacaoNula_LancaArgumentNullException()
    {
        // Arrange
        StudyAreaWeekAssessment[] assessments = [null!];

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => GoalAchievedEvaluator.EvaluateGlobal(assessments));

        // Assert
        Assert.Contains("assessment", exception.Message);
    }

    private static StudyAreaWeekAssessment CreateAssessment(
        decimal individualGoal,
        int minutesStudied)
    {
        return new StudyAreaWeekAssessment(
            individualGoal,
            Guid.NewGuid(),
            minutesStudied);
    }
}
