using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Enums;

public enum StudyPlanStatus
{
    Active,
    Inactive
}

public static class StudyPlanStatusExtensions
{
    public static StudyPlanStatus Parse(string value)
    {
        if (string.Equals(value, "active", StringComparison.OrdinalIgnoreCase))
        {
            return StudyPlanStatus.Active;
        }

        if (string.Equals(value, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            return StudyPlanStatus.Inactive;
        }

        throw DomainRuleViolationException.R04_InvalidStudyPlanStatus(value);
    }

    public static string ToStorageValue(this StudyPlanStatus status)
    {
        return status switch
        {
            StudyPlanStatus.Active => "active",
            StudyPlanStatus.Inactive => "inactive",
            _ => throw DomainRuleViolationException.R04_InvalidStudyPlanStatus(status.ToString())
        };
    }
}