namespace StudyTime.Application.StudyAreaWeeks;

public sealed record CreateStudyAreaWeekRequest(
            Guid StudyAreaId, 
            Guid StudyPlanId, 
            DateOnly WeekStartDate
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