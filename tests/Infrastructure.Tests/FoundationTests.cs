using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

namespace Infrastructure.Tests;

public sealed class FoundationTests
{
    private static StudyTimeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudyTimeDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=studytime;Username=studytime;Password=studytime")
            .Options;

        return new StudyTimeDbContext(options);
    }

    [Fact]
    public void DbContextShouldExposeAllPersistenceEntities()
    {
        using var context = CreateContext();

        Assert.NotNull(context.StudyAreas);
        Assert.NotNull(context.StudyPlans);
        Assert.NotNull(context.StudyAreaWeeks);
        Assert.NotNull(context.StudyAreaWeekAssessments);
        Assert.NotNull(context.WeeklyAssessments);
        Assert.NotNull(context.StudyRecords);
    }

    [Fact]
    public void StudyAreaShouldHaveRequiredStructuralConstraints()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType("StudyTime.Domain.Entities.StudyArea")!;

        Assert.Equal("tb_study_area", entity.GetTableName());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == "Name");

        var property = entity.FindProperty("StdWeekStudyTime")!;
        Assert.False(property.IsNullable);
        Assert.Equal("integer", property.GetColumnType());
    }

    [Fact]
    public void StudyPlanShouldPersistStatusAsString()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType("StudyTime.Domain.Entities.StudyPlan")!;
        var property = entity.FindProperty("Status")!;

        Assert.False(property.IsNullable);
        Assert.Equal("varchar(20)", property.GetColumnType());
        Assert.NotNull(property.GetValueConverter());
    }

    [Fact]
    public void WeeklyAssessmentShouldHaveUniqueIsoWeek()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType("StudyTime.Domain.Entities.WeeklyAssessment")!;
        var index = entity.GetIndexes().Single(x => x.IsUnique && x.Properties.Count == 2);

        Assert.Equal(["Year", "WeekNumber"], index.Properties.Select(x => x.Name));
    }

    [Fact]
    public void StudyAreaWeekShouldHaveUniqueAreaAndWeekStartDate()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType("StudyTime.Domain.Entities.StudyAreaWeek")!;
        var index = entity.GetIndexes().Single(x => x.IsUnique && x.Properties.Count == 2);

        Assert.Equal(["StudyAreaId", "WeekStartDate"], index.Properties.Select(x => x.Name));
    }

    [Fact]
    public void StudyAreaWeekAssessmentShouldHaveUniqueStudyAreaWeek()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType("StudyTime.Domain.Entities.StudyAreaWeekAssessment")!;
        var index = entity.GetIndexes().Single(x => x.IsUnique && x.Properties.Single().Name == "StudyAreaWeekId");

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void StudyRecordShouldHaveCascadeDeleteToStudyAreaWeek()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType("StudyTime.Domain.Entities.StudyRecord")!;
        var foreignKey = entity.GetForeignKeys().Single();

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }
}