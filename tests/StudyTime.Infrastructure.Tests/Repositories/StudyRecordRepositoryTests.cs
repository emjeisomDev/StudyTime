using Npgsql;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;
using StudyTime.Infrastructure.Persistence.Repositories;

namespace StudyTime.Infrastructure.Tests.Repositories;

public sealed class StudyRecordRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public StudyRecordRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_validStudyRecord_persistsStudyRecord()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 1);
        StudyRecord studyRecord = new(
            Guid.NewGuid(),
            new DateOnly(2100, 1, 3),
            new Minutes(45),
            studyAreaWeekId);

        StudyRecordRepository repository = new(context);

        await repository.AddAsync(studyRecord);
        int affectedRows = await context.SaveChangesAsync();

        StudyRecord? persistedStudyRecord =
            await repository.GetByIdAsync(studyRecord.Id);

        Assert.Equal(1, affectedRows);
        Assert.NotNull(persistedStudyRecord);
        Assert.Equal(studyRecord.Id, persistedStudyRecord!.Id);
        Assert.Equal(studyRecord.Date, persistedStudyRecord.Date);
        Assert.Equal(studyRecord.Minutes.Value, persistedStudyRecord.Minutes.Value);
        Assert.Equal(
            studyRecord.StudyAreaWeekId,
            persistedStudyRecord.StudyAreaWeekId);
    }

    [Fact]
    public async Task GetByStudyAreaWeekIdOrderedByCreatedAtDescending_multipleRecords_returnsLifoOrder()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 2);

        Guid oldestId = Guid.NewGuid();
        Guid middleId = Guid.NewGuid();
        Guid newestId = Guid.NewGuid();

        await InsertStudyRecordAsync(
            context,
            oldestId,
            studyAreaWeekId,
            new DateOnly(2100, 1, 3),
            new DateTime(2100, 1, 3, 8, 0, 0, DateTimeKind.Utc),
            20);

        await InsertStudyRecordAsync(
            context,
            middleId,
            studyAreaWeekId,
            new DateOnly(2100, 1, 3),
            new DateTime(2100, 1, 3, 9, 0, 0, DateTimeKind.Utc),
            30);

        await InsertStudyRecordAsync(
            context,
            newestId,
            studyAreaWeekId,
            new DateOnly(2100, 1, 3),
            new DateTime(2100, 1, 3, 10, 0, 0, DateTimeKind.Utc),
            40);

        StudyRecordRepository repository = new(context);

        IReadOnlyList<StudyRecord> records =
            await repository.GetByStudyAreaWeekIdOrderedByCreatedAtDescendingAsync(
                studyAreaWeekId);

        Assert.Equal(3, records.Count);
        Assert.Equal(newestId, records[0].Id);
        Assert.Equal(middleId, records[1].Id);
        Assert.Equal(oldestId, records[2].Id);
    }

    [Fact]
    public async Task GetByStudyAreaWeekIdOrderedByCreatedAtDescending_sameCreatedAt_usesIdDescendingAsTieBreaker()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Guid studyAreaWeekId = await CreateStudyAreaWeekAsync(context, 3);

        Guid lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        DateTime createdAt =
            new(2100, 1, 3, 10, 0, 0, DateTimeKind.Utc);

        await InsertStudyRecordAsync(
            context,
            lowerId,
            studyAreaWeekId,
            new DateOnly(2100, 1, 3),
            createdAt,
            20);

        await InsertStudyRecordAsync(
            context,
            higherId,
            studyAreaWeekId,
            new DateOnly(2100, 1, 3),
            createdAt,
            30);

        StudyRecordRepository repository = new(context);

        IReadOnlyList<StudyRecord> records =
            await repository.GetByStudyAreaWeekIdOrderedByCreatedAtDescendingAsync(
                studyAreaWeekId);

        Assert.Equal(2, records.Count);
        Assert.Equal(higherId, records[0].Id);
        Assert.Equal(lowerId, records[1].Id);
    }

    [Fact]
    public async Task GetByStudyAreaWeekIdOrderedByCreatedAtDescending_otherStudyAreaWeekRecords_ignoresOtherRecords()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        Guid firstStudyAreaWeekId = await CreateStudyAreaWeekAsync(context, 4);
        Guid secondStudyAreaWeekId = await CreateStudyAreaWeekAsync(context, 5);

        Guid expectedId = Guid.NewGuid();
        Guid ignoredId = Guid.NewGuid();

        await InsertStudyRecordAsync(
            context,
            expectedId,
            firstStudyAreaWeekId,
            new DateOnly(2100, 1, 3),
            new DateTime(2100, 1, 3, 8, 0, 0, DateTimeKind.Utc),
            20);

        await InsertStudyRecordAsync(
            context,
            ignoredId,
            secondStudyAreaWeekId,
            new DateOnly(2100, 1, 3),
            new DateTime(2100, 1, 3, 12, 0, 0, DateTimeKind.Utc),
            40);

        StudyRecordRepository repository = new(context);

        IReadOnlyList<StudyRecord> records =
            await repository.GetByStudyAreaWeekIdOrderedByCreatedAtDescendingAsync(
                firstStudyAreaWeekId);

        Assert.Single(records);
        Assert.Equal(expectedId, records[0].Id);
    }

    [Fact]
    public async Task AddAsync_nonExistingStudyAreaWeekId_rejectsPersistenceWithForeignKeyViolation()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        StudyRecord studyRecord = new(
            Guid.NewGuid(),
            new DateOnly(2100, 1, 3),
            new Minutes(15),
            Guid.NewGuid());

        StudyRecordRepository repository = new(context);

        await repository.AddAsync(studyRecord);

        DbUpdateException exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());

        PostgresException? postgresException =
            exception.InnerException as PostgresException;

        Assert.NotNull(postgresException);
        Assert.Equal("23503", postgresException!.SqlState);
        Assert.Equal(
            "FK_tb_study_record_tb_study_area_week_study_area_week_id",
            postgresException.ConstraintName);
    }

    private static async Task InsertStudyRecordAsync(
        StudyTimeDbContext context,
        Guid id,
        Guid studyAreaWeekId,
        DateOnly date,
        DateTime createdAt,
        int minutes)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_record
                (id, date, created_at, minutes, study_area_week_id)
            VALUES
                ({0}, {1}, {2}, {3}, {4});
            """,
            id,
            date,
            createdAt,
            minutes,
            studyAreaWeekId);
    }

    private static async Task<Guid> CreateStudyAreaWeekAsync(
        StudyTimeDbContext context,
        int suffix)
    {
        Guid studyAreaId = Guid.NewGuid();
        Guid studyPlanId = Guid.NewGuid();
        Guid weeklyAssessmentId = Guid.NewGuid();
        Guid studyAreaWeekId = Guid.NewGuid();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_area
                (id, name, std_week_study_time)
            VALUES
                ({0}, {1}, {2});
            """,
            studyAreaId,
            $"Repository Test Area {suffix} {studyAreaId:N}",
            300);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_plan
                (id, name, coefficient, status)
            VALUES
                ({0}, {1}, {2}, {3});
            """,
            studyPlanId,
            $"Repository Test Plan {suffix} {studyPlanId:N}",
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
            suffix,
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
            new DateOnly(2100, 1, 4),
            studyAreaId,
            studyPlanId,
            weeklyAssessmentId);

        return studyAreaWeekId;
    }
}