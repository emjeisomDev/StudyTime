using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

namespace StudyTime.Infrastructure.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("studytime")
            .WithUsername("studytime")
            .WithPassword("studytime_dev")
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        DbContextOptions<StudyTimeDbContext> options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        await using StudyTimeDbContext context = new(options);

        await context.Database.MigrateAsync();
    }

    public StudyTimeDbContext CreateDbContext()
    {
        DbContextOptions<StudyTimeDbContext> options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new StudyTimeDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}