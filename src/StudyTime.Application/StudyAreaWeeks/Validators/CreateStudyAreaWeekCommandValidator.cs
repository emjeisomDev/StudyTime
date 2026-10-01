using FluentValidation;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyAreaWeekCommandValidator : AbstractValidator<CreateStudyAreaWeekCommand>
{
    public CreateStudyAreaWeekCommandValidator()
    {
        RuleFor(command => command.StudyAreaId)
            .NotEmpty()
            .WithMessage("StudyAreaId is required.");

        RuleFor(command => command.StudyPlanId)
            .NotEmpty()
            .WithMessage("StudyPlanId is required.");

        RuleFor(command => command.WeekStartDate)
            .NotEmpty()
            .WithMessage("WeekStartDate is required.");

        RuleFor(command => command.WeekStartDate)
            .Must(date => date.DayOfWeek == DayOfWeek.Monday)
            .WithMessage("WeekStartDate must be a Monday.");
    }
}