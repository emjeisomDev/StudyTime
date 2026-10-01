namespace StudyTime.Application.Dtos;

public sealed record StudyPlanDto(
    Guid Id,
    string Name,
    decimal Coefficient,
    string Status);