using FluentValidation;
using StudyTime.Application.StudyRecords.Commands;

namespace StudyTime.Application.Validators;

public sealed class CreateStudyRecordCommandValidator
    : AbstractValidator<CreateStudyRecordCommand>
{
    public CreateStudyRecordCommandValidator()
    {
        RuleFor(command => command.StudyAreaWeekId)
            .NotEmpty()
            .WithMessage("StudyAreaWeekId is required.");

        RuleFor(command => command.Minutes)
            .GreaterThan(0)
            .WithMessage("Minutes must be greater than zero.");
    }
}