using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record CreateStudyAreaRequest(
    [property: Required]
    [property: StringLength(80, MinimumLength = 1)]
    string Name,
    [property: Range(1, int.MaxValue)]
    int StdWeekStudyTime) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Name is required.",
                [nameof(Name)]);
        }
    }
}
