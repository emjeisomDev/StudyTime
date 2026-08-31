using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaWeekAssessmentConfiguration : IEntityTypeConfiguration<StudyAreaWeekAssessment>
{
    public void Configure(EntityTypeBuilder<StudyAreaWeekAssessment> builder)
    {
        builder.ToTable("tb_study_area_week_assessment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

        builder.Property(x => x.WeekIndividualGoal)
        .HasColumnName("week_individual_goal")
        .HasColumnType("numeric(18,2)")
        .IsRequired();

        builder.Property(x => x.MinutesStudied)
                .HasColumnName("minutes_studied")
                .HasColumnType("integer")
                .HasDefaultValue(0)
                .IsRequired();

        builder.Property(x => x.StudyAreaWeekId)
                .HasColumnName("study_area_week_id")
                .HasColumnType("uuid")
                .IsRequired();


        builder.HasIndex(x => x.StudyAreaWeekId)
                .IsUnique()
                .HasDatabaseName("ux_tb_study_area_week_assessment_week");
                
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_study_area_week_assessment_goal_positive", "week_individual_goal > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_study_area_week_assessment_minutes_non_negative", "minutes_studied >= 0"));
    }
}
