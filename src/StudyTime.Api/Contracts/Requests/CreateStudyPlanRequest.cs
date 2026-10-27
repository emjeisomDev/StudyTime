using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record CreateStudyPlanRequest(
    [param: Required, StringLength(80, MinimumLength = 1)] string Name,
    decimal Coefficient) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name is required.", [nameof(Name)]);
    }
}