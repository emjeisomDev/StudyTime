namespace StudyTime.Domain.Exceptions;

public sealed class NoEligibleStudyRecordException : DomainException
{
    public NoEligibleStudyRecordException()
        : base("No eligible study record was found.")
    {
    }

    public NoEligibleStudyRecordException(string message)
        : base(message)
    {
    }

    public NoEligibleStudyRecordException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}