namespace StudyTime.Application.Dtos;

public sealed record PreviousWeekSummaryDto(
    DateOnly WeekStartDate,
    WeeklyAssessmentDto WeeklyAssessment,
    IReadOnlyList<PreviousWeekSummaryItemDto> Items);

public sealed record PreviousWeekSummaryItemDto(
    StudyAreaDto StudyArea,
    StudyPlanDto StudyPlan,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);