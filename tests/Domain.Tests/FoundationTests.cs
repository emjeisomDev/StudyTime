using StudyTime.Domain;

namespace Domain.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void DomainAssemblyShouldHaveExpectedName()
    {
        Assert.Equal("StudyTime.Domain", AssemblyMarker.Name);
    }
}