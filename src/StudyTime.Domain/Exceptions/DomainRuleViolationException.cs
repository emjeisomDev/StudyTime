namespace StudyTime.Domain.Exceptions;

public sealed class DomainRuleViolationException : DomainException
{
    private DomainRuleViolationException(string message, string code) : base(message, code)
    { }

    public static DomainRuleViolationException R03_StdWeekStudyTimeMustBePositive()
        => new DomainRuleViolationException("The standard weekly study time must be greater than zero.", "R03");

    public static DomainRuleViolationException R04_InvalidStudyPlanStatus(string value)
        => new DomainRuleViolationException($"The status of the study plan '{value}' is invalid. Acceptable values are 'active' and 'inactive'.", "R04");

    public static DomainRuleViolationException R05_MinutesMustBePositive()
        => new DomainRuleViolationException("The number of minutes must be greater than zero.", "R05");

    public static DomainRuleViolationException R07_WeekMustStartOnMonday(DateOnly date)
        => new DomainRuleViolationException($"The date '{date:yyyy-MM-dd}' must be a Monday.", "R07");

    public static DomainRuleViolationException R11_GlobalGoalBelowMinimum(decimal sum)
        => new DomainRuleViolationException($"The weekly global goal must be at least 1500 minutes. Current sum: {sum:0.##}.", "R11");

    public static DomainRuleViolationException R13_CoefficientMustBePositive()
        => new DomainRuleViolationException("The study plan coefficient must be greater than zero.", "R13");

    public static DomainRuleViolationException R21_InvalidIsoWeek(int year, int week)
        => new DomainRuleViolationException($"The specified ISO week is invalid. Year: {year}; week: {week}.", "R21");

    public static DomainRuleViolationException R24_GlobalGoalRequiresAtLeastOneStudyAreaWeek()
        => new DomainRuleViolationException("A meta global da semana exige ao menos uma StudyAreaWeek para avaliação, conforme R24.", "R24");

}