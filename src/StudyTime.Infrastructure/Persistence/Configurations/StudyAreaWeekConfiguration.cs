using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaWeekConfiguration : IEntityTypeConfiguration<StudyAreaWeek>
{
    public void Configure(EntityTypeBuilder<StudyAreaWeek> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tb_study_area_week", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_tb_study_area_week_start_date_monday",
                "EXTRACT(ISODOW FROM week_start_date) = 1");
        });

        builder.HasKey(studyAreaWeek => studyAreaWeek.Id);

        builder.Property(studyAreaWeek => studyAreaWeek.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(studyAreaWeek => studyAreaWeek.WeekStartDate)
            .HasColumnName("week_start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(studyAreaWeek => studyAreaWeek.StudyAreaId)
            .HasColumnName("study_area_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(studyAreaWeek => studyAreaWeek.StudyPlanId)
            .HasColumnName("study_plan_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(studyAreaWeek => studyAreaWeek.WeeklyAssessmentId)
            .HasColumnName("weekly_assessment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(studyAreaWeek => new
        {
            studyAreaWeek.StudyAreaId,
            studyAreaWeek.WeekStartDate
        })
        .IsUnique();

        builder.HasOne<StudyArea>()
            .WithMany()
            .HasForeignKey(studyAreaWeek => studyAreaWeek.StudyAreaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<StudyPlan>()
            .WithMany()
            .HasForeignKey(studyAreaWeek => studyAreaWeek.StudyPlanId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<WeeklyAssessment>()
            .WithMany()
            .HasForeignKey(studyAreaWeek => studyAreaWeek.WeeklyAssessmentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}