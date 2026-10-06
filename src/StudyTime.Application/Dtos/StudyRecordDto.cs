namespace StudyTime.Application.Dtos;

public sealed record StudyRecordDto(
    Guid Id,
    DateOnly Date,
    DateTime CreatedAt,
    int Minutes,
    Guid StudyAreaWeekId);