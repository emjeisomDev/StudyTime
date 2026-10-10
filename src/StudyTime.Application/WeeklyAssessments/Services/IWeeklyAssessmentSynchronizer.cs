namespace StudyTime.Application.WeeklyAssessments.Services;

public interface IWeeklyAssessmentSynchronizer
{
    public Task SynchronizeAsync(
        Guid weeklyAssessmentId,
        CancellationToken cancellationToken = default);
}