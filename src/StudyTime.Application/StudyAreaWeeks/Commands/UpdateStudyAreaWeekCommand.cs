using MediatR;
using StudyTime.Application.Abstractions.Messaging;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record UpdateStudyAreaWeekCommand(
    Guid Id,
    Guid StudyAreaId,
    Guid StudyPlanId) : ICommand, IRequest<StudyAreaWeekDto>;