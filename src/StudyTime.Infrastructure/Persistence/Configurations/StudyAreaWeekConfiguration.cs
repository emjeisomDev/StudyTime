using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaWeekConfiguration : IEntityTypeConfiguration<StudyAreaWeek>
{
    public void Configure(EntityTypeBuilder<StudyAreaWeek> builder)
    {
        builder.ToTable("tb_study_area_week");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

        builder.Property(x => x.WeekStartDate)
                .HasColumnName("week_start_date")
                .HasColumnType("date")
                .IsRequired();

        builder.Property(x => x.StudyAreaId)
                .HasColumnName("study_area_id")
                .HasColumnType("uuid")
                .IsRequired();

        builder.Property(x => x.StudyPlanId)
                .HasColumnName("study_plan_id")
                .HasColumnType("uuid")
                .IsRequired();

        builder.Property(x => x.WeeklyAssessmentId)
                .HasColumnName("weekly_assessment_id")
                .HasColumnType("uuid")
                .IsRequired();

        builder.HasIndex(x => new { x.StudyAreaId, x.WeekStartDate })
                .IsUnique()
                .HasDatabaseName("ux_tb_study_area_week_area_date");

        builder.HasOne<StudyArea>()
                .WithMany()
                .HasForeignKey(x => x.StudyAreaId).OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("fk_study_area_week_study_area");

        builder.HasOne<StudyPlan>()
                .WithMany()
                .HasForeignKey(x => x.StudyPlanId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("fk_study_area_week_study_plan");

        builder.HasOne<WeeklyAssessment>()
                .WithMany()
                .HasForeignKey(x => x.WeeklyAssessmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("fk_study_area_week_weekly_assessment");

        builder.HasOne(x => x.Assessment)
                .WithOne().HasForeignKey<StudyAreaWeekAssessment>(x => x.StudyAreaWeekId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_study_area_week_assessment");
                
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_study_area_week_monday", "EXTRACT(ISODOW FROM week_start_date) = 1"));
    }
}