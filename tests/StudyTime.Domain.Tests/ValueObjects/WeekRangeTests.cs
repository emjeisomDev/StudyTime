using Xunit;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class WeekRangeTests
{
    private static readonly DateTime Monday = new(2026, 8, 31, 0, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Constructor_SegundaFeiraAMeiaNoite_CriaIntervalo()
    {
        // Arrange
        DateTime start = Monday;

        // Act
        WeekRange weekRange = new(start);

        // Assert
        Assert.Equal(start, weekRange.Start);
    }

    [Fact]
    public void Constructor_DiaDiferenteDeSegunda_LancaArgumentException()
    {
        // Arrange
        DateTime tuesday = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => { _ = new WeekRange(tuesday); });

        // Assert
        Assert.Equal("start", exception.ParamName);
    }

    [Fact]
    public void Constructor_HorarioDiferenteDeMeiaNoite_LancaArgumentException()
    {
        // Arrange
        DateTime startWithTime = new(2026, 8, 31, 0, 0, 1, DateTimeKind.Unspecified);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => { _ = new WeekRange(startWithTime); });

        // Assert
        Assert.Equal("start", exception.ParamName);
    }

    [Fact]
    public void Constructor_DataOnlyValida_CriaInicioAMeiaNoite()
    {
        // Arrange
        DateOnly startDate = new(2026, 8, 31);

        // Act
        WeekRange weekRange = new(startDate);

        // Assert
        Assert.Equal(startDate.ToDateTime(TimeOnly.MinValue), weekRange.Start);
    }

    [Fact]
    public void End_SemanaValida_RetornaDomingoAs235959()
    {
        // Arrange
        WeekRange weekRange = new(Monday);

        // Act
        DateTime end = weekRange.End;

        // Assert
        Assert.Equal(new DateTime(2026, 9, 6, 23, 59, 59, DateTimeKind.Unspecified), end);
    }

    [Fact]
    public void Contains_LimitesExatos_IncluiInicioEFim()
    {
        // Arrange
        WeekRange weekRange = new(Monday);

        // Act
        bool containsStart = weekRange.Contains(new DateTime(2026, 8, 31, 0, 0, 0));
        bool containsEnd = weekRange.Contains(new DateTime(2026, 9, 6, 23, 59, 59));

        // Assert
        Assert.True(containsStart);
        Assert.True(containsEnd);
    }

    [Fact]
    public void Contains_SegundaAnteriorESegundaSeguinte_RetornaFalse()
    {
        // Arrange
        WeekRange weekRange = new(Monday);
        DateTime previousMonday = weekRange.Start.AddDays(-7);
        DateTime nextMonday = weekRange.End.AddSeconds(1);

        // Act
        bool containsPreviousMonday = weekRange.Contains(previousMonday);
        bool containsNextMonday = weekRange.Contains(nextMonday);

        // Assert
        Assert.False(containsPreviousMonday);
        Assert.False(containsNextMonday);
    }

    [Fact]
    public void ToString_IntervaloValido_FormataInicioEFim()
    {
        // Arrange
        WeekRange weekRange = new(Monday);

        // Act
        string result = weekRange.ToString();

        // Assert
        Assert.Equal("2026-08-31 00:00:00 - 2026-09-06 23:59:59", result);
    }
}