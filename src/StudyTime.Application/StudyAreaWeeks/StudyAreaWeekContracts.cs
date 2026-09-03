namespace StudyTime.Application.StudyAreaWeeks;

public sealed record CreateStudyAreaWeekRequest(
    Guid StudyAreaId, 
    Guid StudyPlanId, 
    DateOnly WeekStartDate
);

public sealed record CreateStudyAreaWeekBatchItem(
    Guid StudyAreaId, 
    Guid StudyPlanId
);

public sealed record CreateStudyAreaWeekBatchRequest(
    DateOnly WeekStartDate, 
    IReadOnlyList<CreateStudyAreaWeekBatchItem> Items
);

public sealed record UpdateStudyAreaWeekRequest(
    Guid? StudyAreaId,
    Guid? StudyPlanId
);

public sealed record StudyAreaWeekResponse(
    Guid Id, 
    Guid StudyAreaId, 
    Guid StudyPlanId, 
    DateOnly WeekStartDate, 
    Guid WeeklyAssessmentId, 
    decimal WeekIndividualGoal, 
    decimal WeekGlobalGoal, 
    int MinutesStudied
);

public sealed record StudyAreaWeekBatchResponse(
    DateOnly WeekStartDate, 
    Guid WeeklyAssessmentId, 
    decimal WeekGlobalGoal, 
    IReadOnlyList<StudyAreaWeekResponse> Items
);

public sealed record StudyAreaWeekAssessmentResponse(
    Guid StudyAreaWeekId,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);