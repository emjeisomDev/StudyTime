namespace StudyTime.Domain.Exceptions;

public sealed class WeeklyGoalNotMetException : DomainException
{
    public override string Code => "WEEKLY_GOAL_NOT_MET";

    public WeeklyGoalNotMetException()
        : base("The current week's global study goal has not been met.")
    {
    }

    public WeeklyGoalNotMetException(string message)
        : base(message)
    {
    }

    public WeeklyGoalNotMetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}