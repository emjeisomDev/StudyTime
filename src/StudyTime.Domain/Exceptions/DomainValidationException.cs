namespace StudyTime.Domain.Exceptions;

public sealed class DomainValidationException : DomainException
{
    public override string Code => "DOMAIN_VALIDATION";

    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}