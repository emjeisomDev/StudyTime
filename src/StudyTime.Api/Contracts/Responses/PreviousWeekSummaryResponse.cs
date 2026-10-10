namespace StudyTime.Api.Contracts.Responses;

public sealed record PreviousWeekSummaryResponse(
    DateOnly WeekStartDate,
    WeeklyAssessmentResponse WeeklyAssessment,
    IReadOnlyList<PreviousWeekSummaryItemResponse> Items);

public sealed record PreviousWeekSummaryItemResponse(
    StudyAreaResponse StudyArea,
    StudyPlanResponse StudyPlan,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);
