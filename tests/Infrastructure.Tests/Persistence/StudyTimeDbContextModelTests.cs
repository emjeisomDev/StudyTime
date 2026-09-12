using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

namespace StudyTime.Infrastructure.Tests.Persistence;

public sealed class StudyTimeDbContextModelTests
{
    [Fact]
    public void Model_Contains_All_Expected_Entities()
    {
        DbContextOptions<StudyTimeDbContext> options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseInMemoryDatabase("studytime-model-test")
                .Options;

        using StudyTimeDbContext context = new(options);

        string[] tableNames =
        [
            context.Model.FindEntityType("StudyTime.Domain.Entities.StudyArea")!
                .GetTableName()!,
            context.Model.FindEntityType("StudyTime.Domain.Entities.StudyPlan")!
                .GetTableName()!,
            context.Model.FindEntityType("StudyTime.Domain.Entities.WeeklyAssessment")!
                .GetTableName()!,
            context.Model.FindEntityType("StudyTime.Domain.Entities.StudyAreaWeek")!
                .GetTableName()!,
            context.Model.FindEntityType("StudyTime.Domain.Entities.StudyAreaWeekAssessment")!
                .GetTableName()!,
            context.Model.FindEntityType("StudyTime.Domain.Entities.StudyRecord")!
                .GetTableName()!
        ];

        tableNames.Should().BeEquivalentTo(
        [
            "tb_study_area",
            "tb_study_plan",
            "tb_weekly_assessment",
            "tb_study_area_week",
            "tb_study_area_week_assessment",
            "tb_study_record"
        ]);
    }
}