namespace StudyTime.Domain.Abstractions;

/// <summary>
/// Provides the current UTC date and time.
/// </summary>
public interface IClock
{
    public DateTime UtcNow { get; }
}