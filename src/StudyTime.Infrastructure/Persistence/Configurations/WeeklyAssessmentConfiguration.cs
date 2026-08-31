using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class WeeklyAssessmentConfiguration : IEntityTypeConfiguration<WeeklyAssessment>
{
    public void Configure(EntityTypeBuilder<WeeklyAssessment> builder)
    {
        builder.ToTable("tb_weekly_assessment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

        builder.Property(x => x.WeekNumber)
                .HasColumnName("week_number")
                .HasColumnType("integer")
                .IsRequired();

        builder.Property(x => x.Year)
                .HasColumnName("year")
                .HasColumnType("integer")
                .IsRequired();

        builder.Property(x => x.WeekGlobalGoal)
                .HasColumnName("week_global_goal")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

        builder.Property(x => x.MinutesStudied)
                .HasColumnName("minutes_studied")
                .HasColumnType("integer")
                .HasDefaultValue(0)
                .IsRequired();

        builder.HasIndex(x => new { x.Year, x.WeekNumber })
                .IsUnique()
                .HasDatabaseName("ux_tb_weekly_assessment_year_week_number");
                
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_weekly_assessment_week_number", "week_number BETWEEN 1 AND 53"));
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_weekly_assessment_year_positive", "year > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_weekly_assessment_global_goal_positive", "week_global_goal > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_weekly_assessment_minutes_studied_non_negative", "minutes_studied >= 0"));
    }
}