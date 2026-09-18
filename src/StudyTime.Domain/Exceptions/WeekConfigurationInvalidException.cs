namespace StudyTime.Domain.Exceptions;

public sealed class WeekConfigurationInvalidException : DomainException
{
    public override string Code => "WEEK_CONFIGURATION_INVALID";

    public WeekConfigurationInvalidException()
        : base("The study record requires a valid study area week configuration.")
    {
    }

    public WeekConfigurationInvalidException(string message)
        : base(message)
    {
    }

    public WeekConfigurationInvalidException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}