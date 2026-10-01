using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record CreateStudyAreaWeekCommand(
    Guid StudyAreaId,
    Guid StudyPlanId,
    DateOnly WeekStartDate) : IRequest<StudyAreaWeekDto>;