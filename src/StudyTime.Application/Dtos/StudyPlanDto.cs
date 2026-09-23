using StudyTime.Domain.Enums;

namespace StudyTime.Application.Dtos;

public sealed record StudyPlanDto(
    Guid Id,
    string Name,
    decimal Coefficient,
    StudyPlanStatus Status);