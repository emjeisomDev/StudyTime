using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record CreateStudyPlanRequest(
    [property: Required]
    [property: StringLength(80, MinimumLength = 1)]
    string Name,
    [property: Range(
        typeof(decimal),
        "0.0000000001",
        "79228162514264337593543950335")]
    decimal Coefficient) : IValidatableObject
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

        if (Coefficient <= 0)
        {
            yield return new ValidationResult(
                "Coefficient must be greater than zero.",
                [nameof(Coefficient)]);
        }
    }
}
