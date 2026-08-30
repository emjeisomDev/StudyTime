using StudyTime.Application;

namespace Application.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ApplicationShouldReferenceDomainAndNotInfrastructureOrApi()
    {
        var references = typeof(AssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.Contains("StudyTime.Domain", references);
        Assert.DoesNotContain("StudyTime.Infrastructure", references);
        Assert.DoesNotContain("StudyTime.Api", references);
    }
}