using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Metadata;

namespace StudyTime.Infrastructure.Tests.Persistence;

public sealed class StudyAreaWeekConfigurationTests
{
    [Fact]
    public void StudyAreaWeekConfiguration_DefinesMondayCheckConstraint()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(StudyAreaWeek));

        Assert.NotNull(entityType);

        var constraint = entityType!
            .GetCheckConstraints()
            .SingleOrDefault(checkConstraint =>
                checkConstraint.Name == "ck_tb_study_area_week_start_date_monday");

        Assert.NotNull(constraint);
        Assert.Equal("EXTRACT(ISODOW FROM week_start_date) = 1", constraint!.Sql);
    }

    [Fact]
    public void StudyAreaWeekConfiguration_DefinesUniqueAreaAndWeekIndex()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(StudyAreaWeek));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .SingleOrDefault(candidate =>
                    candidate.IsUnique &&
                    candidate.Properties.Select(property => property.Name)
                .SequenceEqual(
                [
                    nameof(StudyAreaWeek.StudyAreaId),
                    nameof(StudyAreaWeek.WeekStartDate)
                ]));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void StudyAreaWeekConfiguration_DefinesStudyAreaForeignKey()
    {
        using var context = CreateContext();

        var foreignKey = GetForeignKey(
            context,
            nameof(StudyAreaWeek.StudyAreaId));

        Assert.Equal(
            typeof(StudyArea),
            foreignKey.PrincipalEntityType.ClrType);

        Assert.Equal(
            DeleteBehavior.NoAction,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void StudyAreaWeekConfiguration_DefinesStudyPlanForeignKey()
    {
        using var context = CreateContext();

        var foreignKey = GetForeignKey(
            context,
            nameof(StudyAreaWeek.StudyPlanId));

        Assert.Equal(
            typeof(StudyPlan),
            foreignKey.PrincipalEntityType.ClrType);

        Assert.Equal(
            DeleteBehavior.NoAction,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void StudyAreaWeekConfiguration_DefinesWeeklyAssessmentForeignKey()
    {
        using var context = CreateContext();

        var foreignKey = GetForeignKey(
            context,
            nameof(StudyAreaWeek.WeeklyAssessmentId));

        Assert.Equal(
            typeof(WeeklyAssessment),
            foreignKey.PrincipalEntityType.ClrType);

        Assert.Equal(
            DeleteBehavior.NoAction,
            foreignKey.DeleteBehavior);
    }

    private static IForeignKey GetForeignKey(StudyTimeDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(StudyAreaWeek));

        Assert.NotNull(entityType);

        var foreignKey = entityType!.GetForeignKeys()
            .SingleOrDefault(candidate =>
                candidate.Properties.Count == 1 &&
                candidate.Properties[0].Name == propertyName);

        Assert.NotNull(foreignKey);

        return foreignKey!;
    }

    private static StudyTimeDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Port=5432;" +
                    "Database=studytime;" +
                    "Username=studytime;" +
                    "Password=studytime_dev")
                .Options;

        return new StudyTimeDbContext(options);
    }
}