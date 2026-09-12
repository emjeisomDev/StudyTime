using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaWeekConfiguration : IEntityTypeConfiguration<StudyAreaWeek>
{
    public void Configure(EntityTypeBuilder<StudyAreaWeek> builder)
    {
        builder.ToTable(
            "tb_study_area_week",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_area_week_week_start_date_monday",
                    "EXTRACT(ISODOW FROM week_start_date) = 1");
            });

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entity => entity.WeekStartDate)
            .HasColumnName("week_start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(entity => entity.StudyAreaId)
            .HasColumnName("study_area_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(entity => entity.StudyPlanId)
            .HasColumnName("study_plan_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(entity => entity.WeeklyAssessmentId)
            .HasColumnName("weekly_assessment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(entity => entity.StudyArea)
            .WithMany()
            .HasForeignKey(entity => entity.StudyAreaId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_tb_study_area_week_study_area");

        builder.HasOne(entity => entity.StudyPlan)
            .WithMany()
            .HasForeignKey(entity => entity.StudyPlanId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_tb_study_area_week_study_plan");

        builder.HasOne(entity => entity.WeeklyAssessment)
            .WithMany()
            .HasForeignKey(entity => entity.WeeklyAssessmentId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_tb_study_area_week_weekly_assessment");

        builder.HasIndex(
                entity => new
                {
                    entity.StudyAreaId,
                    entity.WeekStartDate
                })
            .IsUnique()
            .HasDatabaseName("ux_tb_study_area_week_area_start_date");

        builder.HasIndex(entity => entity.WeeklyAssessmentId)
            .HasDatabaseName("ix_tb_study_area_week_weekly_assessment_id");
    }
}