using StudyTime.Application.Abstractions.Messaging;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Enums;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed record ChangeStudyPlanStatusCommand(
    Guid PlanId,
    StudyPlanStatus NewStatus) : ICommand<StudyPlanDto>;