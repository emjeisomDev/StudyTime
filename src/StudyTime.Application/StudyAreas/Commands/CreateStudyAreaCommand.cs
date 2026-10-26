using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed record CreateStudyAreaCommand(
    string Name,
    int StdWeekStudyTime) : ICommand<StudyAreaDto>;