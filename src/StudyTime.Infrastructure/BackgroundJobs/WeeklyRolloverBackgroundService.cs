using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StudyTime.Application.StudyAreaWeeks.AutoCreation;

namespace StudyTime.Infrastructure.BackgroundJobs;

public sealed class WeeklyRolloverBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<WeeklyRolloverBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan ExecutionInterval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<WeeklyRolloverBackgroundService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteRolloverSafelyAsync(stoppingToken);

            try
            {
                await Task.Delay(ExecutionInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ExecuteRolloverSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<AutoCreateNextWeekJob>();
            await job.ExecuteOnceAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
            return;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Weekly rollover execution failed. The background service will retry on its next scheduled execution.");
        }
    }
}