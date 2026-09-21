namespace StudyTime.Domain.Abstractions;

public interface IClock
{
    public DateTimeOffset UtcNow { get; }
    public DateTimeOffset NowInSaoPaulo { get; }
}