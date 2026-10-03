namespace StudyTime.Application.StudyAreaWeeks.AutoCreation;

public sealed class AutoCreateNextWeekJob(IAutoCreateNextWeekService autoCreateNextWeekService)
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    private readonly IAutoCreateNextWeekService _service = autoCreateNextWeekService
        ?? throw new ArgumentNullException(nameof(autoCreateNextWeekService));

    public Task ExecuteOnceAsync(CancellationToken cancellationToken = default)
        => _service.ExecuteAsync(cancellationToken);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(DefaultInterval);
        await _service.ExecuteAsync(cancellationToken);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await _service.ExecuteAsync(cancellationToken);
        }
    }
}