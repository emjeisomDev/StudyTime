using FluentValidation;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Application.Validators;

public sealed class UpdateStudyAreaWeekCommandValidator
    : AbstractValidator<UpdateStudyAreaWeekCommand>
{
    public UpdateStudyAreaWeekCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Id is required.");

        RuleFor(command => command.StudyAreaId)
            .NotEmpty()
            .WithMessage("StudyAreaId is required.");

        RuleFor(command => command.StudyPlanId)
            .NotEmpty()
            .WithMessage("StudyPlanId is required.");
    }
}