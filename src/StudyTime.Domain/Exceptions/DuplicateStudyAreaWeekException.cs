namespace StudyTime.Domain.Exceptions;

public sealed class DuplicateStudyAreaWeekException : DomainException
{
    public override string Code => "DUPLICATE_STUDY_AREA_WEEK";

    public DuplicateStudyAreaWeekException()
        : base("A study area is already configured for the specified week.")
    {
    }

    public DuplicateStudyAreaWeekException(string message)
        : base(message)
    {
    }

    public DuplicateStudyAreaWeekException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}