using Xunit;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

namespace StudyTime.Infrastructure.Tests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
        .WithDatabase("studytime_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using StudyTimeDbContext dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public StudyTimeDbContext CreateDbContext()
    {
        DbContextOptions<StudyTimeDbContext> options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseNpgsql(ConnectionString)
                .EnableDetailedErrors()
                .Options;

        return new StudyTimeDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using StudyTimeDbContext dbContext = CreateDbContext();

        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE " +
            "tb_study_record, " +
            "tb_study_area_week_assessment, " +
            "tb_study_area_week, " +
            "tb_study_area, " +
            "tb_study_plan, " +
            "tb_weekly_assessment " +
            "RESTART IDENTITY CASCADE;");
    }
}

[CollectionDefinition("PostgreSQL integration tests", DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration tests";
}