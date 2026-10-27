using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;
using StudyTime.Infrastructure.Persistence.Repositories;

namespace StudyTime.Infrastructure.Tests.Repositories;

[Collection(PostgreSqlCollection.Name)]
public sealed class RepositoryIntegrationTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    [Fact]
    public async Task GetByWeekStartDate_ExistsForStudyAreaInWeek_RetornaSomenteConfiguracoesDaSemana()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        StudyArea otherArea = new("Física", new Minutes(240));
        await dbContext.StudyAreas.AddAsync(otherArea);
        await dbContext.SaveChangesAsync();

        DateOnly targetWeek = new(2026, 10, 26);
        DateOnly otherWeek = new(2026, 11, 2);
        StudyAreaWeek firstWeek = new(
            targetWeek,
            core.Area.Id,
            core.Plan.Id,
            core.WeeklyAssessment.Id);
        StudyAreaWeek secondWeek = new(
            targetWeek,
            otherArea.Id,
            core.Plan.Id,
            core.WeeklyAssessment.Id);
        StudyAreaWeek thirdWeek = new(
            otherWeek,
            core.Area.Id,
            core.Plan.Id,
            core.WeeklyAssessment.Id);
        await dbContext.StudyAreaWeeks.AddRangeAsync(firstWeek, secondWeek, thirdWeek);
        await dbContext.SaveChangesAsync();

        StudyAreaWeekRepository repository = new(dbContext);

        // Act
        IReadOnlyList<StudyAreaWeek> actualWeeks =
            await repository.GetByWeekStartDateAsync(targetWeek);
        bool exists = await repository.ExistsForStudyAreaInWeekAsync(core.Area.Id, targetWeek);
        bool doesNotExist = await repository.ExistsForStudyAreaInWeekAsync(Guid.NewGuid(), targetWeek);

        // Assert
        Assert.Equal(2, actualWeeks.Count);
        Assert.All(actualWeeks, item => Assert.Equal(targetWeek, item.WeekStartDate));
        Assert.Contains(actualWeeks, item => item.Id == firstWeek.Id);
        Assert.Contains(actualWeeks, item => item.Id == secondWeek.Id);
        Assert.True(exists);
        Assert.False(doesNotExist);
    }

    [Fact]
    public async Task GetByYearAndWeekNumberAsync_AvaliacaoExistente_RetornaAvaliacaoCorreta()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext, year: 2026, weekNumber: 44);
        WeeklyAssessmentRepository repository = new(dbContext);

        // Act
        WeeklyAssessment? actual = await repository.GetByYearAndWeekNumberAsync(2026, 44);
        WeeklyAssessment? missing = await repository.GetByYearAndWeekNumberAsync(2026, 45);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(core.WeeklyAssessment.Id, actual.Id);
        Assert.Equal(2026, actual.Year);
        Assert.Equal(44, actual.WeekNumber);
        Assert.Null(missing);
    }

    [Fact]
    public async Task SelectLastEligibleAsync_OrdenaPorCreatedAtDescEIdDescEmEmpate()
    {
        // Arrange
        await using StudyTimeDbContext dbContext = CreateDbContext();
        SeededCore core = await SeedCoreAsync(dbContext);
        StudyAreaWeek studyAreaWeek = await AddStudyAreaWeekAsync(
            dbContext,
            core,
            new DateOnly(2026, 10, 26));

        Guid firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid thirdId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        DateOnly recordDate = new(2026, 10, 27);
        await dbContext.StudyRecords.AddRangeAsync(
            new StudyRecord(firstId, recordDate, new Minutes(10), studyAreaWeek.Id),
            new StudyRecord(secondId, recordDate, new Minutes(20), studyAreaWeek.Id),
            new StudyRecord(thirdId, recordDate, new Minutes(30), studyAreaWeek.Id));
        await dbContext.SaveChangesAsync();

        DateTime older = new(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc);
        DateTime tiedLatest = new(2026, 10, 26, 11, 0, 0, DateTimeKind.Utc);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE tb_study_record SET created_at = {older} WHERE id = {firstId};");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE tb_study_record SET created_at = {tiedLatest} WHERE id = {secondId};");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE tb_study_record SET created_at = {tiedLatest} WHERE id = {thirdId};");

        // A fresh context avoids identity resolution returning the previously tracked timestamps.
        await using StudyTimeDbContext queryContext = CreateDbContext();
        StudyRecordLifoSelector selector = new(queryContext);

        // Act
        StudyRecord? selected = await selector.SelectLastEligibleAsync(studyAreaWeek.Id);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal(thirdId, selected.Id);
        Assert.Equal(tiedLatest, selected.CreatedAt);
    }
}
