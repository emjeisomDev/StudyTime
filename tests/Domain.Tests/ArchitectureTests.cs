using System.Reflection;

namespace Domain.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainShouldNotReferenceOuterLayers()
    {
        Assembly domainAssembly = typeof(StudyTime.Domain.AssemblyMarker).Assembly;
        string[] references = domainAssembly
                                .GetReferencedAssemblies()
                                .Select(reference => reference.Name ?? string.Empty)
                                .ToArray();

        Assert.DoesNotContain("StudyTime.Application", references);
        Assert.DoesNotContain("StudyTime.Infrastructure", references);
        Assert.DoesNotContain("StudyTime.Api", references);
    }
}
