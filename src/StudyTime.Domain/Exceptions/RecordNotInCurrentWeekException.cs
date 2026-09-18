namespace StudyTime.Domain.Exceptions;

public sealed class RecordNotInCurrentWeekException : DomainException
{
    public override string Code => "RECORD_NOT_IN_CURRENT_WEEK";

    public RecordNotInCurrentWeekException()
        : base("Study records can only be deleted from the current week.")
    {
    }

    public RecordNotInCurrentWeekException(string message)
        : base(message)
    {
    }

    public RecordNotInCurrentWeekException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}