namespace StudyTime.Domain.Exceptions;

public sealed class RecordNotInCurrentWeekException : DomainException
{
    public RecordNotInCurrentWeekException()
        : base("The study record does not belong to the current week.")
    {
    }

    public RecordNotInCurrentWeekException(string message) : base(message)
    {
    }

    public RecordNotInCurrentWeekException
        (string message, Exception innerException) : base(message, innerException)
    {
    }
}