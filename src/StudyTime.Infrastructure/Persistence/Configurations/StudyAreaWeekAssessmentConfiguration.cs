using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaWeekAssessmentConfiguration
    : IEntityTypeConfiguration<StudyAreaWeekAssessment>
{
    public void Configure(EntityTypeBuilder<StudyAreaWeekAssessment> builder)
    {
        builder.ToTable(
            "tb_study_area_week_assessment",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_area_week_assessment_week_individual_goal_positive",
                    "week_individual_goal > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_area_week_assessment_minutes_studied_non_negative",
                    "minutes_studied >= 0");
            });

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entity => entity.WeekIndividualGoal)
            .HasColumnName("week_individual_goal")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(entity => entity.MinutesStudied)
            .HasColumnName("minutes_studied")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(entity => entity.StudyAreaWeekId)
            .HasColumnName("study_area_week_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(entity => entity.StudyAreaWeek)
            .WithOne(entity => entity.Assessment)
            .HasForeignKey<StudyAreaWeekAssessment>(
                entity => entity.StudyAreaWeekId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_tb_study_area_week_assessment_study_area_week");

        builder.HasIndex(entity => entity.StudyAreaWeekId)
            .IsUnique()
            .HasDatabaseName("ux_tb_study_area_week_assessment_study_area_week_id");
    }
}