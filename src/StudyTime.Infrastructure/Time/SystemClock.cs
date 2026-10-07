using StudyTime.Domain.Abstractions;

namespace StudyTime.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}