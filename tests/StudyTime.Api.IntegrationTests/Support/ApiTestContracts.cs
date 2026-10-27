namespace StudyTime.Api.IntegrationTests.Support;

public sealed record StudyAreaApiResponse(
    Guid Id,
    string Name,
    int StdWeekStudyTime);

public sealed record StudyPlanApiResponse(
    Guid Id,
    string Name,
    decimal Coefficient,
    string Status);

public sealed record StudyAreaWeekApiResponse(
    Guid Id,
    DateOnly WeekStartDate,
    Guid StudyAreaId,
    Guid StudyPlanId,
    Guid WeeklyAssessmentId,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);

public sealed record StudyRecordApiResponse(
    Guid Id,
    DateOnly Date,
    DateTime CreatedAt,
    int Minutes,
    Guid StudyAreaWeekId);

public sealed record WeeklyAssessmentApiResponse(
    Guid Id,
    int WeekNumber,
    int Year,
    decimal WeekGlobalGoal,
    int MinutesStudied,
    bool GoalAchieved);

public sealed record PreviousWeekSummaryItemApiResponse(
    StudyAreaApiResponse StudyArea,
    StudyPlanApiResponse StudyPlan,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);

public sealed record PreviousWeekSummaryApiResponse(
    DateOnly WeekStartDate,
    WeeklyAssessmentApiResponse WeeklyAssessment,
    IReadOnlyList<PreviousWeekSummaryItemApiResponse> Items);
