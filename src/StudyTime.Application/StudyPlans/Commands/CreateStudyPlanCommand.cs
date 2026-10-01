using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed record CreateStudyPlanCommand(
    string Name,
    decimal Coefficient) : IRequest<StudyPlanDto>;