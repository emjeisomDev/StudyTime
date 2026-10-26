using Xunit;
using StudyTime.Domain.Services;

namespace StudyTime.Domain.Tests.Services;

public sealed class WeekDateValidatorTests
{
    private static readonly DateOnly WeekStart = new(2026, 8, 31);

    [Theory]
    [InlineData(2026, 8, 31)]
    [InlineData(2026, 9, 6)]
    public void IsDateWithinWeek_LimitesDeSegundaADomingo_RetornaTrue(
        int year,
        int month,
        int day)
    {
        // Arrange
        // DateOnly representa o dia, não o horário. A segunda inicial representa
        // 00:00:00 e o domingo final representa o último dia inclusivo do intervalo.
        DateOnly date = new(year, month, day);

        // Act
        bool result = WeekDateValidator.IsDateWithinWeek(date, WeekStart);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(2026, 8, 29)]
    [InlineData(2026, 8, 30)]
    [InlineData(2026, 9, 7)]
    public void IsDateWithinWeek_DataForaDaSemana_RetornaFalse(
        int year,
        int month,
        int day)
    {
        // Arrange
        // Fronteiras externas: sábado anterior, domingo anterior e segunda seguinte.
        DateOnly date = new(year, month, day);

        // Act
        bool result = WeekDateValidator.IsDateWithinWeek(date, WeekStart);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDateWithinWeek_DataInicialNaoSegunda_LancaArgumentException()
    {
        // Arrange
        DateOnly invalidWeekStart = new(2026, 9, 1);
        DateOnly date = new(2026, 9, 2);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => WeekDateValidator.IsDateWithinWeek(date, invalidWeekStart));

        // Assert
        Assert.Contains("The week must start on a Monday.", exception.Message);
    }

    [Theory]
    [InlineData(2026, 8, 31)]
    [InlineData(2026, 9, 6)]
    public void ValidateDate_DataDentroDosLimites_NaoLancaExcecao(
        int year,
        int month,
        int day)
    {
        // Arrange
        DateOnly date = new(year, month, day);

        // Act
        Exception? exception = Record.Exception(
            () => WeekDateValidator.ValidateDate(date, WeekStart));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(2026, 8, 29)]
    [InlineData(2026, 9, 7)]
    public void ValidateDate_DataForaDaSemana_LancaArgumentOutOfRangeException(
        int year,
        int month,
        int day)
    {
        // Arrange
        DateOnly date = new(year, month, day);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => WeekDateValidator.ValidateDate(date, WeekStart));

        // Assert
        Assert.Contains("The record date must be within the configured week.", exception.Message);
    }

    [Theory]
    [InlineData(1500, true)]
    [InlineData(1800, true)]
    [InlineData(1499, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsValidConfiguration_MetaGlobal_RetornaSeAtingeMinimo(
        decimal globalGoal,
        bool expected)
    {
        // Arrange
        // Act
        bool result = WeekDateValidator.IsValidConfiguration(globalGoal);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1500)]
    [InlineData(1800)]
    public void ValidateConfiguration_MetaGlobalNoMinimoOuAcima_NaoLancaExcecao(
        decimal globalGoal)
    {
        // Arrange
        // Act
        Exception? exception = Record.Exception(
            () => WeekDateValidator.ValidateConfiguration(globalGoal));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(1499)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateConfiguration_MetaGlobalAbaixoDoMinimo_LancaArgumentException(
        decimal globalGoal)
    {
        // Arrange
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => WeekDateValidator.ValidateConfiguration(globalGoal));

        // Assert
        Assert.Contains(
            "The weekly configuration must have a global goal of at least 1500 minutes.",
            exception.Message);
    }

    [Fact]
    public void ValidateRecord_DataEConfiguracaoValidas_NaoLancaExcecao()
    {
        // Arrange
        DateOnly date = new(2026, 9, 6);

        // Act
        Exception? exception = Record.Exception(
            () => WeekDateValidator.ValidateRecord(date, WeekStart, 1500m));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRecord_DataForaDaSemana_LancaArgumentOutOfRangeException()
    {
        // Arrange
        DateOnly date = new(2026, 9, 7);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => WeekDateValidator.ValidateRecord(date, WeekStart, 1500m));

        // Assert
        Assert.Contains("The record date must be within the configured week.", exception.Message);
    }

    [Fact]
    public void ValidateRecord_MetaGlobalAbaixoDoMinimo_LancaArgumentException()
    {
        // Arrange
        DateOnly date = new(2026, 9, 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => WeekDateValidator.ValidateRecord(date, WeekStart, 1499m));

        // Assert
        Assert.Contains(
            "The weekly configuration must have a global goal of at least 1500 minutes.",
            exception.Message);
    }
}
