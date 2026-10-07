using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaWeekAssessmentConfiguration : IEntityTypeConfiguration<StudyAreaWeekAssessment>
{
    public void Configure(EntityTypeBuilder<StudyAreaWeekAssessment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tb_study_area_week_assessment", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_tb_study_area_week_assessment_individual_goal_positive",
                "week_individual_goal > 0");

            tableBuilder.HasCheckConstraint(
                "ck_tb_study_area_week_assessment_minutes_studied_non_negative",
                "minutes_studied >= 0");
        });

        builder.HasKey(assessment => assessment.Id);

        builder.Property(assessment => assessment.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(assessment => assessment.WeekIndividualGoal)
            .HasColumnName("week_individual_goal")
            .HasColumnType("numeric(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(assessment => assessment.MinutesStudied)
            .HasColumnName("minutes_studied")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(assessment => assessment.StudyAreaWeekId)
            .HasColumnName("study_area_week_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(assessment => assessment.StudyAreaWeekId)
            .IsUnique();

        builder.HasOne<StudyAreaWeek>()
            .WithOne()
            .HasForeignKey<StudyAreaWeekAssessment>(
                assessment => assessment.StudyAreaWeekId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}