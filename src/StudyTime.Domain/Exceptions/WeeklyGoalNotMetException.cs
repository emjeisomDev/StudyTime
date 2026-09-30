namespace StudyTime.Domain.Exceptions;

public sealed class WeeklyGoalNotMetException : DomainException
{
    public WeeklyGoalNotMetException()
        : base("The minimum weekly study goal has not been met.")
    {
    }

    public WeeklyGoalNotMetException(string message)
        : base(message)
    {
    }

    public WeeklyGoalNotMetException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}