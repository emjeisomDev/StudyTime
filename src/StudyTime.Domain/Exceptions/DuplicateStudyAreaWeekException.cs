namespace StudyTime.Domain.Exceptions;

public sealed class DuplicateStudyAreaWeekException : DomainException
{
    public DuplicateStudyAreaWeekException() 
        : base("A study area is already configured for this week.")
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