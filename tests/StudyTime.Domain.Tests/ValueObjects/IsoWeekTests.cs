using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class IsoWeekTests
{
    [Fact]
    public void Constructor_AnoPositivoESemanaValida_InstanciaCriada()
    {
        // Arrange
        const int year = 2024;
        const int weekNumber = 1;

        // Act
        IsoWeek isoWeek = new(year, weekNumber);

        // Assert
        Assert.Equal(year, isoWeek.Year);
        Assert.Equal(weekNumber, isoWeek.WeekNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_AnoNaoPositivo_LancaArgumentOutOfRangeException(int year)
    {
        // Arrange
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = new IsoWeek(year, 1); });

        // Assert
        Assert.Equal("year", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(54)]
    public void Constructor_SemanaForaDoIntervaloUmA53_LancaArgumentOutOfRangeException(int weekNumber)
    {
        // Arrange
        const int year = 2024;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = new IsoWeek(year, weekNumber); });

        // Assert
        Assert.Equal("weekNumber", exception.ParamName);
    }

    [Fact]
    public void Constructor_Semana53EmAnoCom52Semanas_LancaArgumentOutOfRangeException()
    {
        // Arrange
        const int year = 2021;
        const int weekNumber = 53;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = new IsoWeek(year, weekNumber); });

        // Assert
        Assert.Equal("weekNumber", exception.ParamName);
    }

    [Fact]
    public void Constructor_Semana53EmAnoCom53Semanas_InstanciaCriada()
    {
        // Arrange
        const int year = 2020;
        const int weekNumber = 53;

        // Act
        IsoWeek isoWeek = new(year, weekNumber);

        // Assert
        Assert.Equal(year, isoWeek.Year);
        Assert.Equal(weekNumber, isoWeek.WeekNumber);
    }

    [Fact]
    public void GetMonday_SemanaIsoConhecida_RetornaSegundaCorreta()
    {
        // Arrange
        IsoWeek isoWeek = new(2024, 10);

        // Act
        DateOnly monday = isoWeek.GetMonday();

        // Assert
        Assert.Equal(new DateOnly(2024, 3, 4), monday);
    }

    [Fact]
    public void ToString_SemanaComUmDigito_FormataAnoESemanaIso()
    {
        // Arrange
        IsoWeek isoWeek = new(2024, 3);

        // Act
        string result = isoWeek.ToString();

        // Assert
        Assert.Equal("2024-W03", result);
    }
}