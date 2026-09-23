using FluentValidation;

using StudyTime.Application.StudyPlans.Commands;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyPlanCommandValidator : AbstractValidator<CreateStudyPlanCommand>
{
    public CreateStudyPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Coefficient).GreaterThan(0m);
        RuleFor(x => x.Status).IsInEnum();
    }
}