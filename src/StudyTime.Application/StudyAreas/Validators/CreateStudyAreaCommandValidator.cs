using FluentValidation;
using StudyTime.Application.StudyAreas.Commands;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyAreaCommandValidator
    : AbstractValidator<CreateStudyAreaCommand>
{
    public CreateStudyAreaCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("StudyArea name is required.")
            .MaximumLength(80)
            .WithMessage("StudyArea name cannot exceed 80 characters.");

        RuleFor(command => command.StdWeekStudyTime)
            .GreaterThan(0)
            .WithMessage(
                "Standard weekly study time must be greater than zero.");
    }
}