using Xunit;
using System.Globalization;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Infrastructure.Time;

namespace StudyTime.Infrastructure.Tests.Services;

public sealed class CurrentWeekProviderTests
{
    [Theory]
    // Sunday 23:59:59 in Sao Paulo, then Monday 00:00:00.
    [InlineData("2026-08-31T02:59:59Z", "2026-08-24")]
    [InlineData("2026-08-31T03:00:00Z", "2026-08-31")]
    // The actual August-to-September boundary: 2025-08-31 was a Sunday.
    [InlineData("2025-09-01T02:59:59Z", "2025-08-25")]
    [InlineData("2025-09-01T03:00:00Z", "2025-09-01")]
    // The last second of Monday in Sao Paulo and the beginning of Tuesday.
    [InlineData("2026-09-01T02:59:59Z", "2026-08-31")]
    [InlineData("2026-09-01T03:00:00Z", "2026-08-31")]
    // New-year boundary: Friday 2027-01-01 still belongs to the week starting in 2026.
    [InlineData("2027-01-01T02:59:59Z", "2026-12-28")]
    [InlineData("2027-01-01T03:00:00Z", "2026-12-28")]
    [InlineData("2027-01-04T02:59:59Z", "2026-12-28")]
    [InlineData("2027-01-04T03:00:00Z", "2027-01-04")]
    // UTC is already on Tuesday, while Sao Paulo is still Monday night.
    [InlineData("2026-10-27T02:30:00Z", "2026-10-26")]
    [InlineData("2026-10-27T03:00:00Z", "2026-10-26")]
    public void GetCurrentWeek_InstanteUtcConvertidoParaSaoPaulo_RetornaInicioExato(
        string utcInstant,
        string expectedStartDate)
    {
        // Arrange
        DateTime utcNow = DateTime.Parse(
            utcInstant,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
        IClock clock = new FakeClock(utcNow);
        CurrentWeekProvider provider = new(clock);
        DateTime expectedStart = DateOnly.ParseExact(
            expectedStartDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue);

        // Act
        WeekRange actual = provider.GetCurrentWeek();

        // Assert
        Assert.Equal(expectedStart, actual.Start);
        Assert.Equal(DateTimeKind.Unspecified, actual.Start.Kind);
    }

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
}
