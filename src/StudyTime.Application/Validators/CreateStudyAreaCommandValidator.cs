using FluentValidation;

using StudyTime.Application.StudyAreas.Commands;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyAreaCommandValidator : AbstractValidator<CreateStudyAreaCommand>
{
    public CreateStudyAreaCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.StdWeekStudyTime)
            .GreaterThan(0);
    }
}