using MediatR;
using StudyTime.Domain.Enums;


namespace StudyTime.Application.StudyPlans.Commands;

public sealed record UpdateStudyPlanStatusCommand(
    Guid Id,
    StudyPlanStatus Status) : IRequest;