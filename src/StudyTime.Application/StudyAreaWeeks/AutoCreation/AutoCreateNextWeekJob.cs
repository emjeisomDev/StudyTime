namespace StudyTime.Application.StudyAreaWeeks.AutoCreation;

/// <summary>
/// Executes one rollover attempt. Scheduling belongs exclusively to
/// WeeklyRolloverBackgroundService, avoiding two competing timer loops.
/// </summary>
public sealed class AutoCreateNextWeekJob(IAutoCreateNextWeekService autoCreateNextWeekService)
{
    private readonly IAutoCreateNextWeekService _service = autoCreateNextWeekService
        ?? throw new ArgumentNullException(nameof(autoCreateNextWeekService));

    public Task ExecuteOnceAsync(CancellationToken cancellationToken = default)
        => _service.ExecuteAsync(cancellationToken);
}