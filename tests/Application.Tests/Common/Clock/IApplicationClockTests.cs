using StudyTime.Application.Common.Clock;

namespace Application.Tests.Common.Clock;

public sealed class IApplicationClockTests
{
    [Fact]
    public void ContractShouldExposeCurrentTimeTodayAndTimezone()
    {
        var properties = typeof(IApplicationClock).GetProperties();

        Assert.Contains(properties, property => property.Name == nameof(IApplicationClock.Now) && property.PropertyType == typeof(DateTimeOffset));
        Assert.Contains(properties, property => property.Name == nameof(IApplicationClock.Today) && property.PropertyType == typeof(DateOnly));
        Assert.Contains(properties, property => property.Name == nameof(IApplicationClock.TimeZone) && property.PropertyType == typeof(TimeZoneInfo));
    }
}