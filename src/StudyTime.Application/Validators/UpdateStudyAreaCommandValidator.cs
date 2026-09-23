using FluentValidation;

using StudyTime.Application.StudyAreas.Commands;

namespace StudyTime.Application.Validators;

public sealed class UpdateStudyAreaCommandValidator : AbstractValidator<UpdateStudyAreaCommand>
{
    public UpdateStudyAreaCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.StdWeekStudyTime).GreaterThan(0);
    }
}