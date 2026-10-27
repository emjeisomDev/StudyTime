using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using StudyTime.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using StudyTime.Application.StudyAreaWeeks.AutoCreation;

namespace StudyTime.Infrastructure.Tests.Services;

public sealed class WeeklyRolloverBackgroundServiceTests
{
    private static readonly TimeSpan WatchdogTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task ExecuteRolloverSafelyAsync_JobConcluiComSucesso_NaoRegistraErro()
    {
        // Arrange
        FakeRolloverService job = new(_ => Task.CompletedTask);
        RecordingLogger logger = new();
        using IHost host = CreateHost(job, logger);

        // Act
        await host.StartAsync();
        await job.InvocationStarted.Task.WaitAsync(WatchdogTimeout);
        await host.StopAsync();

        // Assert
        Assert.Equal(1, job.InvocationCount);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ExecuteRolloverSafelyAsync_JobLancaExcecao_RegistraErroEContinuaSemPropagar()
    {
        // Arrange
        InvalidOperationException expectedException = new("Falha simulada no rollover.");
        FakeRolloverService job = new(_ => Task.FromException(expectedException));
        RecordingLogger logger = new();
        using IHost host = CreateHost(job, logger);

        // Act
        await host.StartAsync();
        await job.InvocationStarted.Task.WaitAsync(WatchdogTimeout);
        (LogLevel Level, Exception? Exception, string Message) loggedError =
            await logger.ErrorLogged.WaitAsync(WatchdogTimeout);
        await host.StopAsync();

        // Assert
        Assert.Equal(1, job.InvocationCount);
        Assert.Equal(LogLevel.Error, loggedError.Level);
        Assert.Same(expectedException, loggedError.Exception);
        Assert.Contains("Weekly rollover execution failed", loggedError.Message);
        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task StopAsync_JobEmExecucaoRecebeCancelamento_NaoRegistraErro()
    {
        // Arrange
        TaskCompletionSource jobWaitingForCancellation = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakeRolloverService job = new(token =>
        {
            jobWaitingForCancellation.TrySetResult();
            return Task.Delay(Timeout.InfiniteTimeSpan, token);
        });
        RecordingLogger logger = new();
        using IHost host = CreateHost(job, logger);

        // Act
        await host.StartAsync();
        await jobWaitingForCancellation.Task.WaitAsync(WatchdogTimeout);
        await host.StopAsync();

        // Assert
        Assert.Equal(1, job.InvocationCount);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task StopAsync_AposJobBemSucedidoCancelaDelay_NaoRegistraErro()
    {
        // Arrange
        FakeRolloverService job = new(_ => Task.CompletedTask);
        RecordingLogger logger = new();
        using IHost host = CreateHost(job, logger);

        // Act
        await host.StartAsync();
        await job.InvocationStarted.Task.WaitAsync(WatchdogTimeout);
        await host.StopAsync();

        // Assert
        Assert.Equal(1, job.InvocationCount);
        Assert.Empty(logger.Entries);
    }

    private static IHost CreateHost(FakeRolloverService job, RecordingLogger logger)
    {
        return new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<ILogger<WeeklyRolloverBackgroundService>>(logger);
                services.AddScoped<IAutoCreateNextWeekService>(_ => job);
                services.AddScoped<AutoCreateNextWeekJob>();
                services.AddSingleton<WeeklyRolloverBackgroundService>();
                services.AddHostedService<WeeklyRolloverBackgroundService>(
                    provider => provider.GetRequiredService<WeeklyRolloverBackgroundService>());
            })
            .Build();
    }

    private sealed class FakeRolloverService(Func<CancellationToken, Task> execute)
        : IAutoCreateNextWeekService
    {
        private int _invocationCount;

        public TaskCompletionSource<CancellationToken> InvocationStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _invocationCount);
            InvocationStarted.TrySetResult(cancellationToken);
            await execute(cancellationToken);
        }
    }

    private sealed class RecordingLogger : ILogger<WeeklyRolloverBackgroundService>
    {
        private readonly ConcurrentQueue<(LogLevel Level, Exception? Exception, string Message)> _entries = new();
        private readonly TaskCompletionSource<(LogLevel Level, Exception? Exception, string Message)> _errorLogged = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyCollection<(LogLevel Level, Exception? Exception, string Message)> Entries =>
            _entries.ToArray();

        public Task<(LogLevel Level, Exception? Exception, string Message)> ErrorLogged => _errorLogged.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            (LogLevel Level, Exception? Exception, string Message) entry =
                (logLevel, exception, formatter(state, exception));
            _entries.Enqueue(entry);

            if (logLevel == LogLevel.Error)
            {
                _errorLogged.TrySetResult(entry);
            }
        }
    }
}