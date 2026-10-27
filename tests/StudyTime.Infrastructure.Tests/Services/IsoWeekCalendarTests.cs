using Xunit;
using System.Globalization;
using StudyTime.Domain.ValueObjects;
using StudyTime.Infrastructure.Calendar;

namespace StudyTime.Infrastructure.Tests.Services;

public sealed class IsoWeekCalendarTests
{
    [Theory]
    [InlineData("2020-12-28", 2020, 53)]
    [InlineData("2021-01-01", 2020, 53)]
    [InlineData("2021-01-04", 2021, 1)]
    [InlineData("2022-01-03", 2022, 1)]
    [InlineData("2024-12-30", 2025, 1)]
    [InlineData("2025-12-29", 2026, 1)]
    [InlineData("2026-12-28", 2026, 53)]
    [InlineData("2026-12-29", 2026, 53)]
    [InlineData("2027-01-04", 2027, 1)]
    public void GetIsoWeek_DataDeFronteira_RetornaAnoESemanaIsoCorretos(
        string dateText,
        int expectedYear,
        int expectedWeekNumber)
    {
        // Arrange
        IsoWeekCalendar calendar = new();
        DateOnly date = DateOnly.ParseExact(
            dateText,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        // Act
        IsoWeek actual = calendar.GetIsoWeek(date);

        // Assert
        Assert.Equal(expectedYear, actual.Year);
        Assert.Equal(expectedWeekNumber, actual.WeekNumber);
    }

    [Theory]
    [InlineData(2020, 53, "2020-12-28")]
    [InlineData(2021, 1, "2021-01-04")]
    [InlineData(2026, 53, "2026-12-28")]
    [InlineData(2027, 1, "2027-01-04")]
    public void GetMonday_AnoESemanaIsoValidos_RetornaSegundaCorreta(
        int year,
        int weekNumber,
        string expectedMondayText)
    {
        // Arrange
        IsoWeekCalendar calendar = new();
        DateOnly expectedMonday = DateOnly.ParseExact(
            expectedMondayText,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        // Act
        DateOnly actual = calendar.GetMonday(year, weekNumber);

        // Assert
        Assert.Equal(expectedMonday, actual);
        Assert.Equal(DayOfWeek.Monday, actual.DayOfWeek);
    }
}
