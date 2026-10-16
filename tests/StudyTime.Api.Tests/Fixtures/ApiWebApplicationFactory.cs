using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using StudyTime.Domain.Abstractions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using StudyTime.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StudyTime.Api.Tests.Fixtures;

public sealed class FakeClock : IClock
{
    private long _utcTicks;

    public FakeClock() : this(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc)) { }

    public FakeClock(DateTime utcNow)
    {
        SetUtcNow(utcNow);
    }

    public DateTime UtcNow => new(Interlocked.Read(ref _utcTicks), DateTimeKind.Utc);

    public void SetUtcNow(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The fake clock requires a UTC DateTime.", nameof(utcNow));
        }

        Interlocked.Exchange(ref _utcTicks, utcNow.Ticks);
    }

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        SetUtcNow(utcNow.UtcDateTime);
    }
}

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("studytime_tests")
            .WithUsername("studytime_test")
            .WithPassword("studytime_test_password")
            .Build();

    private bool _postgresStarted;

    public FakeClock Clock { get; } = new();

    public async Task StartPostgresAsync(
        CancellationToken cancellationToken = default)
    {
        if (_postgresStarted)
        {
            return;
        }

        await _postgres.StartAsync(cancellationToken);
        _postgresStarted = true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        _postgres.GetConnectionString()
                });
        });

        builder.ConfigureTestServices(services =>
        {
            // Força o DbContext de integração a usar o PostgreSQL do Testcontainers.
            // O registro original pode ter capturado a connection string do appsettings.
            services.RemoveAll<IDbContextOptionsConfiguration<StudyTimeDbContext>>();
            services.RemoveAll<DbContextOptions<StudyTimeDbContext>>();
            services.RemoveAll<StudyTimeDbContext>();
            services.AddDbContext<StudyTimeDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }

    public async Task EnsureDatabaseCreatedAsync(
        CancellationToken cancellationToken = default)
    {
        EnsurePostgresStarted();

        await using var scope = Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<StudyTimeDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task ResetDatabaseAsync(
        CancellationToken cancellationToken = default)
    {
        EnsurePostgresStarted();

        await using var scope = Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<StudyTimeDbContext>();

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private void EnsurePostgresStarted()
    {
        if (!_postgresStarted)
        {
            throw new InvalidOperationException(@"Start PostgreSQL by calling StartPostgresAsync before creating or resetting the test database.");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            base.Dispose(disposing);
            _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        base.Dispose(false);
    }
}