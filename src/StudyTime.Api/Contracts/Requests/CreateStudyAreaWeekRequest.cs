using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record CreateStudyAreaWeekRequest(Guid StudyAreaId, Guid StudyPlanId, DateOnly WeekStartDate) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StudyAreaId == Guid.Empty)
        {
            yield return new ValidationResult(
                "StudyAreaId is required.",
                [nameof(StudyAreaId)]);
        }

        if (StudyPlanId == Guid.Empty)
        {
            yield return new ValidationResult(
                "StudyPlanId is required.",
                [nameof(StudyPlanId)]);
        }

        if (WeekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            yield return new ValidationResult(
                "WeekStartDate must be a Monday.",
                [nameof(WeekStartDate)]);
        }
    }
}
