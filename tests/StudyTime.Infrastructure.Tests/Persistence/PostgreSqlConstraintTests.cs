using Xunit;
using Npgsql;
using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.ValueObjects;
using System.Runtime.CompilerServices;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;

namespace StudyTime.Infrastructure.Tests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlConstraintTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    [Fact]
    public async Task InserirMetaPadraoZero_FalhaComCheckViolation23514()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        Minutes invalidZeroMinutes = (Minutes)RuntimeHelpers.GetUninitializedObject(typeof(Minutes));
        Assert.Equal(0, invalidZeroMinutes.Value);
        StudyArea invalidArea = new(Guid.NewGuid(), "Área inválida", invalidZeroMinutes);
        await dbContext.StudyAreas.AddAsync(invalidArea);

        // Act
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        // Assert
        PostgresException postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23514", postgresException.SqlState);
    }

    [Fact]
    public async Task InserirNomeDeStudyAreaDuplicado_FalhaComUniqueViolation23505()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        _ = await SeedCoreAsync(dbContext, areaName: "Matemática");
        StudyArea duplicate = new("Matemática", new Minutes(240));
        await dbContext.StudyAreas.AddAsync(duplicate);

        // Act
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        // Assert
        PostgresException postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23505", postgresException.SqlState);
    }

    [Fact]
    public async Task InserirStudyAreaWeekComTercaFeira_FalhaNoCheckIsodow23514()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        DateOnly tuesday = new(2026, 10, 27);
        Guid weekId = Guid.NewGuid();

        // Act: raw SQL bypasses the domain constructor, so PostgreSQL's CHECK is tested directly.
        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(
            () => dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tb_study_area_week
                    (id, week_start_date, study_area_id, study_plan_id, weekly_assessment_id)
                VALUES ({weekId}, {tuesday}, {core.Area.Id}, {core.Plan.Id}, {core.WeeklyAssessment.Id});
                """));

        // Assert
        Assert.Equal("23514", exception.SqlState);
    }

    [Fact]
    public async Task InserirDuasStudyAreaWeekComMesmaAreaESemana_FalhaComUniqueViolation23505()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        DateOnly monday = new(2026, 10, 26);
        _ = await AddStudyAreaWeekAsync(dbContext, core, monday);
        StudyAreaWeek duplicate = new(
            monday,
            core.Area.Id,
            core.Plan.Id,
            core.WeeklyAssessment.Id);
        await dbContext.StudyAreaWeeks.AddAsync(duplicate);

        // Act
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        // Assert
        PostgresException postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23505", postgresException.SqlState);
    }

    [Fact]
    public async Task InserirWeeklyAssessmentComAnoESemanaDuplicados_FalhaComUniqueViolation23505()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        _ = await SeedCoreAsync(dbContext, year: 2026, weekNumber: 44);
        WeeklyAssessment duplicate = new(44, 2026, 1800m);
        await dbContext.WeeklyAssessments.AddAsync(duplicate);

        // Act
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        // Assert
        PostgresException postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23505", postgresException.SqlState);
    }

    [Fact]
    public async Task InserirStudyAreaWeekComForeignKeyInexistente_FalhaComForeignKeyViolation23503()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        StudyAreaWeek invalidForeignKey = new(
            new DateOnly(2026, 10, 26),
            Guid.NewGuid(),
            core.Plan.Id,
            core.WeeklyAssessment.Id);
        await dbContext.StudyAreaWeeks.AddAsync(invalidForeignKey);

        // Act
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        // Assert
        PostgresException postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23503", postgresException.SqlState);
    }

    [Fact]
    public async Task ExcluirStudyAreaWeek_ComCascadeRemoveRegistrosEAvaliacaoIndividual()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        StudyAreaWeek studyAreaWeek = await AddStudyAreaWeekAsync(
            dbContext,
            core,
            new DateOnly(2026, 10, 26));
        StudyAreaWeekAssessment individualAssessment = new(180m, studyAreaWeek.Id);
        StudyRecord studyRecord = new(new DateOnly(2026, 10, 27), new Minutes(30), studyAreaWeek.Id);
        await dbContext.StudyAreaWeekAssessments.AddAsync(individualAssessment);
        await dbContext.StudyRecords.AddAsync(studyRecord);
        await dbContext.SaveChangesAsync();

        // Act: execute DELETE against PostgreSQL to verify the database's ON DELETE CASCADE.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM tb_study_area_week WHERE id = {studyAreaWeek.Id};");

        // Assert
        await using StudyTimeDbContext verificationContext = CreateDbContext();
        Assert.False(await verificationContext.StudyRecords.AnyAsync(item => item.Id == studyRecord.Id));
        Assert.False(await verificationContext.StudyAreaWeekAssessments
            .AnyAsync(item => item.Id == individualAssessment.Id));
    }

    [Fact]
    public async Task ExcluirStudyAreaComStudyAreaWeekReferenciando_FalhaComForeignKeyViolation23503()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        _ = await AddStudyAreaWeekAsync(dbContext, core, new DateOnly(2026, 10, 26));

        // Act: execute DELETE directly against PostgreSQL to verify the foreign-key constraint.
        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(
            () => dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM tb_study_area WHERE id = {core.Area.Id};"));

        // Assert
        Assert.Equal("23503", exception.SqlState);
    }
}
