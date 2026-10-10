namespace StudyTime.Api.Contracts.Responses;

public sealed record StudyRecordResponse(
    Guid Id,
    DateOnly Date,
    DateTime CreatedAt,
    int Minutes,
    Guid StudyAreaWeekId);
