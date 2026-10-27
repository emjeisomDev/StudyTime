using Xunit;
using System.Data.Common;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;

namespace StudyTime.Infrastructure.Tests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class EntityMappingTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    [Fact]
    public async Task SalvarERelerEntidades_PreservaValoresEMapeamentosPostgreSql()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext, standardWeeklyMinutes: 180, coefficient: 1.25m);
        DateOnly monday = new(2026, 10, 26);
        DateOnly recordDate = new(2026, 10, 27);
        StudyAreaWeek studyAreaWeek = new(
            monday,
            core.Area.Id,
            core.Plan.Id,
            core.WeeklyAssessment.Id);
        StudyAreaWeekAssessment individualAssessment = new(180m, studyAreaWeek.Id, 35);
        StudyRecord studyRecord = new(recordDate, new Minutes(45), studyAreaWeek.Id);

        await dbContext.StudyAreaWeeks.AddAsync(studyAreaWeek);
        await dbContext.StudyAreaWeekAssessments.AddAsync(individualAssessment);
        await dbContext.StudyRecords.AddAsync(studyRecord);

        // Act
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        StudyArea loadedArea = Assert.IsType<StudyArea>(
            await dbContext.StudyAreas.SingleAsync(item => item.Id == core.Area.Id));
        StudyPlan loadedPlan = Assert.IsType<StudyPlan>(
            await dbContext.StudyPlans.SingleAsync(item => item.Id == core.Plan.Id));
        WeeklyAssessment loadedWeeklyAssessment = Assert.IsType<WeeklyAssessment>(
            await dbContext.WeeklyAssessments.SingleAsync(item => item.Id == core.WeeklyAssessment.Id));
        StudyAreaWeek loadedWeek = Assert.IsType<StudyAreaWeek>(
            await dbContext.StudyAreaWeeks.SingleAsync(item => item.Id == studyAreaWeek.Id));
        StudyAreaWeekAssessment loadedIndividualAssessment = Assert.IsType<StudyAreaWeekAssessment>(
            await dbContext.StudyAreaWeekAssessments.SingleAsync(item => item.Id == individualAssessment.Id));
        StudyRecord loadedRecord = Assert.IsType<StudyRecord>(
            await dbContext.StudyRecords.SingleAsync(item => item.Id == studyRecord.Id));

        // Assert
        Assert.Equal(180, loadedArea.StdWeekStudyTime.Value);
        Assert.Equal(1.25m, loadedPlan.Coefficient.Value);
        Assert.Equal(core.WeeklyAssessment.WeekNumber, loadedWeeklyAssessment.WeekNumber);
        Assert.Equal(core.WeeklyAssessment.Year, loadedWeeklyAssessment.Year);
        Assert.Equal(1500m, loadedWeeklyAssessment.WeekGlobalGoal);
        Assert.Equal(monday, loadedWeek.WeekStartDate);
        Assert.Equal(180m, loadedIndividualAssessment.WeekIndividualGoal);
        Assert.Equal(35, loadedIndividualAssessment.MinutesStudied);
        Assert.Equal(recordDate, loadedRecord.Date);
        Assert.Equal(45, loadedRecord.Minutes.Value);
        Assert.Equal(studyAreaWeek.Id, loadedRecord.StudyAreaWeekId);
        Assert.Equal(DateTimeKind.Utc, loadedRecord.CreatedAt.Kind);

        Assert.Equal("integer", await ReadScalarStringAsync(
            dbContext,
            "SELECT data_type FROM information_schema.columns " +
            "WHERE table_name = 'tb_study_area' AND column_name = 'std_week_study_time';"));
        Assert.Equal("numeric:3:2", await ReadScalarStringAsync(
            dbContext,
            "SELECT concat(data_type, ':', numeric_precision, ':', numeric_scale) " +
            "FROM information_schema.columns " +
            "WHERE table_name = 'tb_study_plan' AND column_name = 'coefficient';"));
        Assert.Equal("date", await ReadScalarStringAsync(
            dbContext,
            "SELECT data_type FROM information_schema.columns " +
            "WHERE table_name = 'tb_study_area_week' AND column_name = 'week_start_date';"));
        Assert.Equal("date", await ReadScalarStringAsync(
            dbContext,
            "SELECT data_type FROM information_schema.columns " +
            "WHERE table_name = 'tb_study_record' AND column_name = 'date';"));
        Assert.Equal("timestamp with time zone", await ReadScalarStringAsync(
            dbContext,
            "SELECT data_type FROM information_schema.columns " +
            "WHERE table_name = 'tb_study_record' AND column_name = 'created_at';"));
    }

    [Fact]
    public async Task InserirStudyRecordSemDataEHorario_UsaDefaultsDoPostgreSql()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        StudyAreaWeek studyAreaWeek = await AddStudyAreaWeekAsync(
            dbContext,
            core,
            new DateOnly(2026, 10, 26));
        Guid recordId = Guid.NewGuid();

        // Act: date and created_at are intentionally omitted from the INSERT.
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tb_study_record (id, minutes, study_area_week_id)
            VALUES ({recordId}, {45}, {studyAreaWeek.Id});
            """);

        StudyRecord record = await dbContext.StudyRecords
            .AsNoTracking()
            .SingleAsync(item => item.Id == recordId);
        DateOnly expectedLocalDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(record.CreatedAt, DateTimeKind.Utc),
                TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

        // Assert
        Assert.Equal(expectedLocalDate, record.Date);
        Assert.NotEqual(default, record.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, record.CreatedAt.Kind);
        Assert.Equal(45, record.Minutes.Value);
    }

    private static async Task<string> ReadScalarStringAsync(
        StudyTimeDbContext dbContext,
        string sql)
    {
        await dbContext.Database.OpenConnectionAsync();
        try
        {
            await using DbCommand command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            object? value = await command.ExecuteScalarAsync();
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
