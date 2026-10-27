using Xunit;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using StudyTime.Infrastructure.Persistence;

namespace StudyTime.Infrastructure.Tests.Fixtures;

public abstract class PostgreSqlTestBase : IAsyncLifetime
{
    protected PostgreSqlTestBase(PostgreSqlFixture fixture)
    {
        Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    protected PostgreSqlFixture Fixture { get; }

    protected StudyTimeDbContext CreateDbContext() => Fixture.CreateDbContext();

    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected static async Task<SeededCore> SeedCoreAsync(
        StudyTimeDbContext dbContext,
        string areaName = "Matemática",
        int standardWeeklyMinutes = 180,
        string planName = "Plano principal",
        decimal coefficient = 1.25m,
        int year = 2026,
        int weekNumber = 44,
        decimal globalGoal = 1500m)
    {
        StudyArea area = new(areaName, new Minutes(standardWeeklyMinutes));
        StudyPlan plan = new(planName, new Coefficient(coefficient));
        WeeklyAssessment weeklyAssessment = new(weekNumber, year, globalGoal);

        await dbContext.StudyAreas.AddAsync(area);
        await dbContext.StudyPlans.AddAsync(plan);
        await dbContext.WeeklyAssessments.AddAsync(weeklyAssessment);
        await dbContext.SaveChangesAsync();

        return new SeededCore(area, plan, weeklyAssessment);
    }

    protected static async Task<StudyAreaWeek> AddStudyAreaWeekAsync(
        StudyTimeDbContext dbContext,
        SeededCore core,
        DateOnly weekStartDate,
        Guid? id = null)
    {
        StudyAreaWeek studyAreaWeek = id is null
            ? new StudyAreaWeek(
                weekStartDate,
                core.Area.Id,
                core.Plan.Id,
                core.WeeklyAssessment.Id)
            : new StudyAreaWeek(
                id.Value,
                weekStartDate,
                core.Area.Id,
                core.Plan.Id,
                core.WeeklyAssessment.Id);

        await dbContext.StudyAreaWeeks.AddAsync(studyAreaWeek);
        await dbContext.SaveChangesAsync();
        return studyAreaWeek;
    }

    protected sealed record SeededCore(
        StudyArea Area,
        StudyPlan Plan,
        WeeklyAssessment WeeklyAssessment);
}
