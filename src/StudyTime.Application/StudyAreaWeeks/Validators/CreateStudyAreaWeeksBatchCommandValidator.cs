using FluentValidation;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyAreaWeeksBatchCommandValidator
    : AbstractValidator<IReadOnlyCollection<StudyAreaWeekBatchItemDto>>
{
    public CreateStudyAreaWeeksBatchCommandValidator()
    {
        RuleFor(items => items)
            .NotNull()
            .WithMessage("Items are required.");

        RuleFor(items => items)
            .NotEmpty()
            .WithMessage("At least one item is required.")
            .When(items => items is not null);

        RuleForEach(items => items)
            .ChildRules(item =>
            {
                item.RuleFor(dto => dto.StudyAreaId)
                    .NotEmpty()
                    .WithMessage("StudyAreaId is required.");

                item.RuleFor(dto => dto.StudyPlanId)
                    .NotEmpty()
                    .WithMessage("StudyPlanId is required.");
            })
            .When(items => items is not null);
    }
}