using Npgsql;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;

namespace StudyTime.Infrastructure.Tests.Persistence;

public sealed class StudyRecordConfigurationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public StudyRecordConfigurationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertMinutesZero_WhenExecuted_ThrowsPostgresException()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();
        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 1);
        Guid studyRecordId = Guid.NewGuid();

        Func<Task> action = () => context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO tb_study_record
                    (id, date, created_at, minutes, study_area_week_id)
                VALUES
                    ({0}, CURRENT_DATE, CURRENT_TIMESTAMP, {1}, {2});
                """,
                studyRecordId,
                0,
                studyAreaWeekId);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(action);
        Assert.Equal("ck_tb_study_record_minutes_positive", exception.ConstraintName);
    }

    [Fact]
    public async Task InsertWithoutDate_WhenExecuted_UsesCurrentSaoPauloDate()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 2);
        Guid studyRecordId = Guid.NewGuid();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_record
                (id, minutes, study_area_week_id)
            VALUES
                ({0}, {1}, {2});
            """,
            studyRecordId,
            30,
            studyAreaWeekId);

        DateOnly persistedDate = await context.Database.SqlQueryRaw<DateOnly>(
                """
                SELECT date AS "Value"
                FROM tb_study_record
                WHERE id = {0}
                """,
                studyRecordId)
            .SingleAsync();

        DateOnly expectedDate = await context.Database.SqlQueryRaw<DateOnly>(
                """
                SELECT (NOW() AT TIME ZONE 'America/Sao_Paulo')::date AS "Value"
                """)
            .SingleAsync();

        Assert.Equal(expectedDate, persistedDate);
    }

    [Fact]
    public async Task DeleteStudyAreaWeek_WhenStudyRecordExists_CascadesStudyRecordDeletion()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();
        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 3);
        Guid studyRecordId = Guid.NewGuid();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_record
                (id, date, created_at, minutes, study_area_week_id)
            VALUES
                ({0}, CURRENT_DATE, CURRENT_TIMESTAMP, {1}, {2});
            """,
            studyRecordId,
            30,
            studyAreaWeekId);

        int deletedStudyAreaWeeks = await context.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM tb_study_area_week
                WHERE id = {0};
                """,
                studyAreaWeekId);

        Assert.Equal(1, deletedStudyAreaWeeks);

        int remainingStudyRecords = await context.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::integer AS "Value"
                FROM tb_study_record
                WHERE id = {0}
                """,
                studyRecordId)
            .SingleAsync();

        Assert.Equal(0, remainingStudyRecords);
    }

    [Fact]
    public async Task InsertSameDateAndStudyAreaWeek_WhenRepeated_AllowsMultipleStudyRecords()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 4);
        Guid firstStudyRecordId = Guid.NewGuid();
        Guid secondStudyRecordId = Guid.NewGuid();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_record
                (id, date, created_at, minutes, study_area_week_id)
            VALUES
                ({0}, DATE '2026-10-05', CURRENT_TIMESTAMP, {1}, {2});
            """,
            firstStudyRecordId,
            30,
            studyAreaWeekId);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_record
                (id, date, created_at, minutes, study_area_week_id)
            VALUES
                ({0}, DATE '2026-10-05', CURRENT_TIMESTAMP, {1}, {2});
            """,
            secondStudyRecordId,
            45,
            studyAreaWeekId);

        int recordCount = await context.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::integer AS "Value"
                FROM tb_study_record
                WHERE study_area_week_id = {0}
                  AND date = DATE '2026-10-05'
                """,
                studyAreaWeekId)
            .SingleAsync();

        Assert.Equal(2, recordCount);
    }

    private static async Task<Guid> CreateStudyAreaWeekAsync(
        StudyTimeDbContext context,
        int weekNumber)
    {
        Guid studyAreaId = Guid.NewGuid();
        Guid studyPlanId = Guid.NewGuid();
        Guid weeklyAssessmentId = Guid.NewGuid();
        Guid studyAreaWeekId = Guid.NewGuid();

        DateOnly weekStartDate =
            new DateOnly(2100, 1, 4).AddDays((weekNumber - 1) * 7);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_area
                (id, name, std_week_study_time)
            VALUES
                ({0}, {1}, {2});
            """,
            studyAreaId,
            $"Test Area {studyAreaId:N}",
            300);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_plan
                (id, name, coefficient, status)
            VALUES
                ({0}, {1}, {2}, {3});
            """,
            studyPlanId,
            $"Test Plan {studyPlanId:N}",
            1.00m,
            "active");

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_weekly_assessment
                (id, week_number, year, week_global_goal, minutes_studied)
            VALUES
                ({0}, {1}, {2}, {3}, {4});
            """,
            weeklyAssessmentId,
            weekNumber,
            2100,
            1500m,
            0);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_area_week
                (id, week_start_date, study_area_id, study_plan_id, weekly_assessment_id)
            VALUES
                ({0}, {1}, {2}, {3}, {4});
            """,
            studyAreaWeekId,
            weekStartDate,
            studyAreaId,
            studyPlanId,
            weeklyAssessmentId);

        return studyAreaWeekId;
    }
}