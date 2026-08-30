using StudyTime.Infrastructure;

namespace Infrastructure.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void InfrastructureAssemblyShouldHaveExpectedName()
    {
        Assert.Equal("StudyTime.Infrastructure", AssemblyMarker.Name);
    }
}