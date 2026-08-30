using StudyTime.Application;

namespace Application.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void ApplicationAssemblyShouldHaveExpectedName()
    {
        Assert.Equal("StudyTime.Application", AssemblyMarker.Name);
    }
}
