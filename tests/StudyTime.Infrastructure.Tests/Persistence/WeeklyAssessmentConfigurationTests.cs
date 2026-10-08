using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;

namespace StudyTime.Infrastructure.Tests.Persistence;

public sealed class WeeklyAssessmentConfigurationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public WeeklyAssessmentConfigurationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertWeekNumberZero_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Func<Task> action = () => context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_weekly_assessment
                    (id, week_number, year, week_global_goal, minutes_studied)
                VALUES
                    ({0}, {1}, {2}, {3}, {4});
                """,
                Guid.NewGuid(),
                0,
                2100,
                1500m,
                0);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("ck_tb_weekly_assessment_week_number_valid", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertWeekNumber54_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Func<Task> action = () => context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_weekly_assessment
                    (id, week_number, year, week_global_goal, minutes_studied)
                VALUES
                    ({0}, {1}, {2}, {3}, {4});
                """,
                Guid.NewGuid(),
                54,
                2100,
                1500m,
                0);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("ck_tb_weekly_assessment_week_number_valid", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertYearZero_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Func<Task> action = () => context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_weekly_assessment
                    (id, week_number, year, week_global_goal, minutes_studied)
                VALUES
                    ({0}, {1}, {2}, {3}, {4});
                """,
                Guid.NewGuid(),
                1,
                0,
                1500m,
                0);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("ck_tb_weekly_assessment_year_positive", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertGlobalGoalZero_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Func<Task> action = () =>
            context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_weekly_assessment
                    (id, week_number, year, week_global_goal, minutes_studied)
                VALUES
                    ({0}, {1}, {2}, {3}, {4});
                """,
                Guid.NewGuid(),
                1,
                2101,
                0m,
                0);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("ck_tb_weekly_assessment_global_goal_positive", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertNegativeMinutesStudied_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Func<Task> action = () => context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_weekly_assessment
                    (id, week_number, year, week_global_goal, minutes_studied)
                VALUES
                    ({0}, {1}, {2}, {3}, {4});
                """,
                Guid.NewGuid(),
                1,
                2102,
                1500m,
                -1);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("ck_tb_weekly_assessment_minutes_studied_non_negative", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertDuplicateYearAndWeekNumber_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_weekly_assessment
                (id, week_number, year, week_global_goal, minutes_studied)
            VALUES
                ({0}, {1}, {2}, {3}, {4});
            """,
            Guid.NewGuid(),
            1,
            2103,
            1500m,
            0);

        Func<Task> action = () => context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_weekly_assessment
                    (id, week_number, year, week_global_goal, minutes_studied)
                VALUES
                    ({0}, {1}, {2}, {3}, {4});
                """,
                Guid.NewGuid(),
                1,
                2103,
                1800m,
                10);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("IX_tb_weekly_assessment_year_week_number", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertDifferentWeekNumberWithSameYear_WhenExecuted_AllowsPersistence()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_weekly_assessment
                (id, week_number, year, week_global_goal, minutes_studied)
            VALUES
                ({0}, {1}, {2}, {3}, {4});
            """,
            Guid.NewGuid(),
            1,
            2104,
            1500m,
            0);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_weekly_assessment
                (id, week_number, year, week_global_goal, minutes_studied)
            VALUES
                ({0}, {1}, {2}, {3}, {4});
            """,
            Guid.NewGuid(),
            2,
            2104,
            1600m,
            30);

        int count = await context.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::integer AS "Value"
                FROM tb_weekly_assessment
                WHERE year = {0}
                """,
                2104)
            .SingleAsync();

        Assert.Equal(2, count);
    }
}