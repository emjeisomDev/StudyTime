namespace StudyTime.Api.Contracts.Responses;

public sealed record PreviousWeekSummaryResponse(
    DateOnly WeekStartDate,
    WeeklyAssessmentResponse WeeklyAssessment,
    IReadOnlyList<PreviousWeekSummaryItemResponse> Items);