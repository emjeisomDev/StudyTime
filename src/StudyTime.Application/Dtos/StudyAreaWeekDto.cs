namespace StudyTime.Application.Dtos;

public sealed record StudyAreaWeekDto(
    Guid Id,
    DateOnly WeekStartDate,
    Guid StudyAreaId,
    Guid StudyPlanId,
    Guid WeeklyAssessmentId,
    decimal WeekIndividualGoal,
    int MinutesStudied,
    bool GoalAchieved);