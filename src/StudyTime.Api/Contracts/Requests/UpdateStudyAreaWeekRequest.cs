using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record UpdateStudyAreaWeekRequest(Guid StudyAreaId, Guid StudyPlanId) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StudyAreaId == Guid.Empty)
        {
            yield return new ValidationResult("StudyAreaId is required.", [nameof(StudyAreaId)]);
        }

        if (StudyPlanId == Guid.Empty)
        {
            yield return new ValidationResult("StudyPlanId is required.", [nameof(StudyPlanId)]);
        }
    }
}
