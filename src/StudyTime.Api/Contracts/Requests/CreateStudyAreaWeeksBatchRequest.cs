using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record CreateStudyAreaWeeksBatchRequest(DateOnly WeekStartDate, IReadOnlyCollection<StudyAreaWeekItemRequest> Items) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (WeekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            yield return new ValidationResult(
                "WeekStartDate must be a Monday.",
                [nameof(WeekStartDate)]);
        }

        if (Items is null || Items.Count == 0)
        {
            yield return new ValidationResult(
                "At least one item is required.",
                [nameof(Items)]);

            yield break;
        }

        var seenStudyAreaIds = new HashSet<Guid>();

        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items.ElementAt(index);

            if (item is null)
            {
                yield return new ValidationResult(
                    $"Item at index {index} cannot be null.",
                    [nameof(Items)]);

                continue;
            }

            if (item.StudyAreaId == Guid.Empty)
            {
                yield return new ValidationResult(
                    $"StudyAreaId is required for item at index {index}.",
                    [nameof(Items)]);
            }
            else if (!seenStudyAreaIds.Add(item.StudyAreaId))
            {
                yield return new ValidationResult(
                    $"StudyAreaId '{item.StudyAreaId}' occurs more than once.",
                    [nameof(Items)]);
            }

            if (item.StudyPlanId == Guid.Empty)
            {
                yield return new ValidationResult(
                    $"StudyPlanId is required for item at index {index}.",
                    [nameof(Items)]);
            }
        }
    }
}

public sealed record StudyAreaWeekItemRequest(
    Guid StudyAreaId,
    Guid StudyPlanId);