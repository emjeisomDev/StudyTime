using Xunit;
using StudyTime.Infrastructure.Time;

namespace StudyTime.Infrastructure.Tests.Services;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ConsultaRelogioDoSistema_RetornaDateTimeComKindUtc()
    {
        // Arrange
        SystemClock clock = new();

        // Act
        DateTime actual = clock.UtcNow;

        // Assert
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }
}
