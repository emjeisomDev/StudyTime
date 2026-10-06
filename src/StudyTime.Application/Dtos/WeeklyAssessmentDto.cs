namespace StudyTime.Application.Dtos;

public sealed record WeeklyAssessmentDto(
    Guid Id,
    int WeekNumber,
    int Year,
    decimal WeekGlobalGoal,
    int MinutesStudied,
    bool GoalAchieved);