using MediatR;
using StudyTime.Application.Abstractions.Messaging;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record CreateStudyAreaWeeksBatchCommand(
    DateOnly WeekStartDate,
    IReadOnlyCollection<StudyAreaWeekBatchItemDto> Items)
    : ICommand, IRequest<IReadOnlyList<StudyAreaWeekDto>>;