namespace StudyTime.Domain.Exceptions;

public sealed class NoEligibleStudyRecordException : DomainException
{
    public override string Code => "NO_ELIGIBLE_STUDY_RECORD";

    public NoEligibleStudyRecordException()
        : base("No eligible study record was found for the requested operation.")
    {
    }

    public NoEligibleStudyRecordException(string message)
        : base(message)
    {
    }

    public NoEligibleStudyRecordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}