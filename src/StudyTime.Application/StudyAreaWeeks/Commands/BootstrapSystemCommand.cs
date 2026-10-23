using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed record BootstrapSystemCommand
    (IReadOnlyCollection<StudyAreaWeekBatchItemDto> Items) 
    : ICommand<IReadOnlyList<StudyAreaWeekDto>>;