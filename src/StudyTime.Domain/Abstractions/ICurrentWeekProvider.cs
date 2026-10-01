using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Abstractions;

/// <summary>
/// Provides the current week using the official application timezone.
/// </summary>
public interface ICurrentWeekProvider
{
    public WeekRange GetCurrentWeek();
}