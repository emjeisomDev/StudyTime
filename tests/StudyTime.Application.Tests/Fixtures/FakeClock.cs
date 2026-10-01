using StudyTime.Domain.Abstractions;

namespace StudyTime.Application.Tests.Fixtures;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; private set; }

    public FakeClock() : this(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc))
    {
    }

    public FakeClock(DateTime utcNow)
    {
        SetUtcNow(utcNow);
    }

    public void SetUtcNow(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The supplied date and time must have UTC kind.", nameof(utcNow));
        }

        UtcNow = utcNow;
    }

    public void Advance(TimeSpan amount)
    {
        UtcNow = UtcNow.Add(amount);
    }
}