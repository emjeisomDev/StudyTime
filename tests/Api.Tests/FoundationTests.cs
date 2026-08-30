namespace Api.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void ApiAssemblyShouldHaveExpectedName()
    {
        Assert.Equal("StudyTime.Api", typeof(Program).Assembly.GetName().Name);
    }
}
