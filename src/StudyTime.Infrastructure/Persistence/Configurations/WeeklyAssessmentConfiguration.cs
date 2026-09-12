using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class WeeklyAssessmentConfiguration : IEntityTypeConfiguration<WeeklyAssessment>
{
    public void Configure(EntityTypeBuilder<WeeklyAssessment> builder)
    {
        builder.ToTable(
            "tb_weekly_assessment",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tb_weekly_assessment_week_number_valid",
                    "week_number BETWEEN 1 AND 53");

                tableBuilder.HasCheckConstraint(
                    "ck_tb_weekly_assessment_year_positive",
                    "year > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_tb_weekly_assessment_week_global_goal_positive",
                    "week_global_goal > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_tb_weekly_assessment_minutes_studied_non_negative",
                    "minutes_studied >= 0");
            });

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entity => entity.WeekNumber)
            .HasColumnName("week_number")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(entity => entity.Year)
            .HasColumnName("year")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(entity => entity.WeekGlobalGoal)
            .HasColumnName("week_global_goal")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(entity => entity.MinutesStudied)
            .HasColumnName("minutes_studied")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasIndex(
                entity => new
                {
                    entity.Year,
                    entity.WeekNumber
                })
            .IsUnique()
            .HasDatabaseName("ux_tb_weekly_assessment_year_week");
    }
}