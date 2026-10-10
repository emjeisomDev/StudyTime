namespace StudyTime.Api.Contracts.Responses;

public sealed record StudyAreaWeekResponse(
    Guid Id,
    DateOnly WeekStartDate,
    Guid StudyAreaId,
    Guid StudyPlanId,
    Guid WeeklyAssessmentId,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);
