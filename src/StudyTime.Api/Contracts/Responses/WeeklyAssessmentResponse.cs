namespace StudyTime.Api.Contracts.Responses;

public sealed record WeeklyAssessmentResponse(
    Guid Id,
    int WeekNumber,
    int Year,
    decimal WeekGlobalGoal,
    int MinutesStudied,
    bool GoalAchieved);
