using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed record CreateStudyAreaCommand(
    string Name,
    int StdWeekStudyTime) : IRequest<StudyAreaDto>;