namespace StudyTime.Domain.Exceptions;

public sealed class WeekConfigurationLockedException : DomainException
{
    public WeekConfigurationLockedException()
        : base("The weekly configuration is locked.")
    {
    }

    public WeekConfigurationLockedException(string message)
        : base(message)
    {
    }

    public WeekConfigurationLockedException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}