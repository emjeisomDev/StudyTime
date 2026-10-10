namespace StudyTime.Api.Contracts.Responses;

public sealed record StudyPlanResponse(
    Guid Id,
    string Name,
    decimal Coefficient,
    string Status);
