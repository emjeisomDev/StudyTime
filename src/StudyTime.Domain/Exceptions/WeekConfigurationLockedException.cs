namespace StudyTime.Domain.Exceptions;

public sealed class WeekConfigurationLockedException : DomainException
{
    public override string Code => "WEEK_CONFIGURATION_LOCKED";

    public WeekConfigurationLockedException()
        : base("The week configuration is locked until the current week's global goal is met.")
    {
    }

    public WeekConfigurationLockedException(string message)
        : base(message)
    {
    }

    public WeekConfigurationLockedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}