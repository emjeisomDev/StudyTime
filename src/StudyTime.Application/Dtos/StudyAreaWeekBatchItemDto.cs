namespace StudyTime.Application.Dtos;

public sealed record StudyAreaWeekBatchItemDto(
    Guid StudyAreaId,
    Guid StudyPlanId);