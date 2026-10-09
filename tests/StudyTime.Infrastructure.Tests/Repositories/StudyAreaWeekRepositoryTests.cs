using Npgsql;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;
using StudyTime.Infrastructure.Persistence.Repositories;

namespace StudyTime.Infrastructure.Tests.Repositories;

public sealed class StudyAreaWeekRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public StudyAreaWeekRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByWeekStartDate_multipleAreasSameWeek_returnsAllAreasOrderedByStudyAreaId()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        DateOnly weekStartDate = new(2100, 1, 4);

        Guid firstStudyAreaId = Guid.Parse(
            "00000000-0000-0000-0000-000000000001");

        Guid secondStudyAreaId = Guid.Parse(
            "00000000-0000-0000-0000-000000000002");

        Guid firstStudyAreaWeekId = await CreateStudyAreaWeekAsync(
            context,
            firstStudyAreaId,
            weekStartDate,
            1);

        Guid secondStudyAreaWeekId = await CreateStudyAreaWeekAsync(
            context,
            secondStudyAreaId,
            weekStartDate,
            2);

        StudyAreaWeekRepository repository =
            new(context);

        IReadOnlyList<StudyTime.Domain.Entities.StudyAreaWeek> results =
            await repository.GetByWeekStartDateAsync(weekStartDate);

        Assert.Equal(2, results.Count);
        Assert.Equal(firstStudyAreaWeekId, results[0].Id);
        Assert.Equal(secondStudyAreaWeekId, results[1].Id);
        Assert.All(
            results,
            studyAreaWeek =>
                Assert.Equal(weekStartDate, studyAreaWeek.WeekStartDate));
    }

    [Fact]
    public async Task ExistsForStudyAreaInWeek_existingCombination_returnsTrue()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        DateOnly weekStartDate = new(2100, 1, 11);
        Guid studyAreaId = Guid.NewGuid();

        await CreateStudyAreaWeekAsync(
            context,
            studyAreaId,
            weekStartDate,
            3);

        StudyAreaWeekRepository repository =
            new(context);

        bool exists =
            await repository.ExistsForStudyAreaInWeekAsync(
                studyAreaId,
                weekStartDate);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsForStudyAreaInWeek_differentStudyAreaOrWeek_returnsFalse()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        DateOnly configuredWeek = new(2100, 1, 18);
        DateOnly differentWeek = new(2100, 1, 25);
        Guid configuredStudyAreaId = Guid.NewGuid();
        Guid differentStudyAreaId = Guid.NewGuid();

        await CreateStudyAreaWeekAsync(
            context,
            configuredStudyAreaId,
            configuredWeek,
            4);

        StudyAreaWeekRepository repository =
            new(context);

        bool differentStudyArea =
            await repository.ExistsForStudyAreaInWeekAsync(
                differentStudyAreaId,
                configuredWeek);

        bool differentWeekResult =
            await repository.ExistsForStudyAreaInWeekAsync(
                configuredStudyAreaId,
                differentWeek);

        Assert.False(differentStudyArea);
        Assert.False(differentWeekResult);
    }

    [Fact]
    public async Task AddDuplicateStudyAreaWeek_sameAreaAndWeek_rejectsPersistenceWithUniqueViolation()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        DateOnly weekStartDate = new(2100, 2, 1);
        Guid studyAreaId = Guid.NewGuid();

        await CreateStudyAreaWeekAsync(
            context,
            studyAreaId,
            weekStartDate,
            5);

        Guid duplicateStudyAreaWeekId = Guid.NewGuid();
        Guid studyPlanId = Guid.NewGuid();
        Guid weeklyAssessmentId = Guid.NewGuid();

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_plan
                (id, name, coefficient, status)
            VALUES
                ({0}, {1}, {2}, {3});
            """,
            studyPlanId,
            $"Duplicate Repository Test Plan {studyPlanId:N}",
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
            6,
            2101,
            1500m,
            0);

        StudyTime.Domain.Entities.StudyAreaWeek duplicate =
            new(
                duplicateStudyAreaWeekId,
                weekStartDate,
                studyAreaId,
                studyPlanId,
                weeklyAssessmentId);

        StudyAreaWeekRepository repository =
            new(context);

        await repository.AddAsync(duplicate);

        DbUpdateException exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());

        PostgresException? postgresException =
            exception.InnerException as PostgresException;

        Assert.NotNull(postgresException);
        Assert.Equal("23505", postgresException!.SqlState);
        Assert.Equal(
            "IX_tb_study_area_week_study_area_id_week_start_date",
            postgresException.ConstraintName);
    }

    private static async Task<Guid> CreateStudyAreaWeekAsync(
        StudyTimeDbContext context,
        Guid studyAreaId,
        DateOnly weekStartDate,
        int suffix)
    {
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
            $"Area Week Repository Test {suffix} {studyAreaId:N}",
            300);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO tb_study_plan
                (id, name, coefficient, status)
            VALUES
                ({0}, {1}, {2}, {3});
            """,
            studyPlanId,
            $"Area Week Repository Plan {suffix} {studyPlanId:N}",
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
            2101,
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
