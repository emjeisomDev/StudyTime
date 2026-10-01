using FluentValidation;
using StudyTime.Application.StudyPlans.Commands;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyPlanCommandValidator
    : AbstractValidator<CreateStudyPlanCommand>
{
    public CreateStudyPlanCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("StudyPlan name is required.")
            .MaximumLength(80)
            .WithMessage("StudyPlan name cannot exceed 80 characters.");

        RuleFor(command => command.Coefficient)
            .GreaterThan(0)
            .WithMessage("Coefficient must be greater than zero.")
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Coefficient must have at most two decimal places.")
            .Must(FitDecimalThreeTwo)
            .WithMessage("Coefficient must fit the DECIMAL(3,2) database precision.");
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal value)
    {
        return decimal.Round(value, 2) == value;
    }

    private static bool FitDecimalThreeTwo(decimal value)
    {
        return value > 0 && value < 10;
    }
}