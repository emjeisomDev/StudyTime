namespace StudyTime.Infrastructure;

/// <summary>
/// Identifies the StudyTime infrastructure assembly for architecture and foundation tests.
/// </summary>
public static class AssemblyMarker
{
    /// <summary>
    /// Gets the name of the assembly containing the infrastructure layer.
    /// </summary>
    public static string Name => typeof(AssemblyMarker).Assembly.GetName().Name ?? "StudyTime.Infrastructure";
}