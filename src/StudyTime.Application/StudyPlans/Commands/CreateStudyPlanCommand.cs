using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed record CreateStudyPlanCommand(
    string Name,
    decimal Coefficient) : ICommand<StudyPlanDto>;