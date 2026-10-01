using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed record UpdateStudyAreaCommand(
    Guid Id,
    string? Name = null,
    int? StdWeekStudyTime = null) : IRequest<StudyAreaDto>;