using FluentValidation;

using StudyTime.Application.StudyPlans.Commands;

namespace StudyTime.Application.Validators;

public sealed class UpdateStudyPlanCommandValidator
    : AbstractValidator<ChangeStudyPlanStatusCommand>
{
    public UpdateStudyPlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}