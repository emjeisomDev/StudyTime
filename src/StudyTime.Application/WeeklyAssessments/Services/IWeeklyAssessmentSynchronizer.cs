namespace StudyTime.Application.WeeklyAssessments.Services;

public interface IWeeklyAssessmentSynchronizer
{
    Task SynchronizeAsync(
        Guid weeklyAssessmentId,
        CancellationToken cancellationToken = default);
}