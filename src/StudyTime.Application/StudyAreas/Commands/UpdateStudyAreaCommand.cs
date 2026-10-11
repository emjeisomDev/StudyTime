using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed record UpdateStudyAreaCommand(
    Guid Id,
    string? Name = null,
    int? StdWeekStudyTime = null) : ICommand<StudyAreaDto>;