namespace StudyTime.Application.StudyAreaWeeks.AutoCreation;

public interface IAutoCreateNextWeekService
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default);
}
