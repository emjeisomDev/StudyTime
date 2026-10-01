namespace StudyTime.Application.Dtos;

public sealed record StudyAreaDto(
    Guid Id,
    string Name,
    int StdWeekStudyTime);