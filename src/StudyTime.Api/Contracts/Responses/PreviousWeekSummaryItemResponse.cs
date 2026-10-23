namespace StudyTime.Api.Contracts.Responses;
public sealed record PreviousWeekSummaryItemResponse(
    StudyAreaResponse StudyArea,
    StudyPlanResponse StudyPlan,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);