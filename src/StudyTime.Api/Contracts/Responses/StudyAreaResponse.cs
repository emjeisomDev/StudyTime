namespace StudyTime.Api.Contracts.Responses;

public sealed record StudyAreaResponse(
    Guid Id,
    string Name,
    int StdWeekStudyTime);
