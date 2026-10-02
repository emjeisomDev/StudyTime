using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record BootstrapSystemCommand(
    IReadOnlyCollection<StudyAreaWeekBatchItemDto> Items)
    : IRequest<IReadOnlyList<StudyAreaWeekDto>>;