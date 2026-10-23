using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record CreateStudyAreaWeekCommand(
    Guid StudyAreaId, 
    Guid StudyPlanId, 
    DateOnly WeekStartDate) : ICommand<StudyAreaWeekDto>;