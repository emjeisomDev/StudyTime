using FluentValidation;
using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed record CreateStudyAreaCommand(
    string Name,
    int StdWeekStudyTime) : IRequest<StudyAreaDto>;

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
            .WithMessage("Standard weekly study time must be greater than zero.");
    }
}