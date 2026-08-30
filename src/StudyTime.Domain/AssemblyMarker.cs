namespace StudyTime.Domain;

/// <summary>
/// Identifies the StudyTime domain assembly for architecture and foundation tests.
/// </summary>
public static class AssemblyMarker
{
    /// <summary>
    /// Gets the name of the assembly containing the domain layer.
    /// </summary>
    public static string Name => typeof(AssemblyMarker).Assembly.GetName().Name ?? "StudyTime.Domain";
}