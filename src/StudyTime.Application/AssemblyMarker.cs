namespace StudyTime.Application;

/// <summary>
/// Identifies the StudyTime application assembly for architecture and foundation tests.
/// </summary>
public static class AssemblyMarker
{
    /// <summary>
    /// Gets the name of the assembly containing the application layer.
    /// </summary>
    public static string Name => typeof(AssemblyMarker).Assembly.GetName().Name ?? "StudyTime.Application";

    /// <summary>
    /// Gets the name of the domain assembly referenced by the application layer.
    /// </summary>
    public static string DomainAssemblyName => typeof(StudyTime.Domain.AssemblyMarker).Assembly.GetName().Name ?? "StudyTime.Domain";
}