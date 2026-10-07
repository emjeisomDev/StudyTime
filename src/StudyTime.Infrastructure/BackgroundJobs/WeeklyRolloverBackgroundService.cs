using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudyTime.Application.StudyAreaWeeks.AutoCreation;

namespace StudyTime.Infrastructure.BackgroundJobs;

public sealed class WeeklyRolloverBackgroundService(
    AutoCreateNextWeekJob autoCreateNextWeekJob,
    ILogger<WeeklyRolloverBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan ExecutionInterval = TimeSpan.FromHours(1);

    private readonly AutoCreateNextWeekJob _autoCreateNextWeekJob = autoCreateNextWeekJob
        ?? throw new ArgumentNullException(nameof(autoCreateNextWeekJob));

    private readonly ILogger<WeeklyRolloverBackgroundService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteRolloverSafelyAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(ExecutionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ExecuteRolloverSafelyAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _autoCreateNextWeekJob.ExecuteOnceAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                @"Weekly rollover execution failed. 
                The background service will continue and retry on the next scheduled execution.");
        }
    }
}