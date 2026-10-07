using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tb_study_plan", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_tb_study_plan_coefficient_positive",
                "coefficient > 0");

            tableBuilder.HasCheckConstraint(
                "ck_tb_study_plan_status_valid",
                "status IN ('active', 'inactive')");
        });

        builder.HasKey(studyPlan => studyPlan.Id);

        builder.Property(studyPlan => studyPlan.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(studyPlan => studyPlan.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(studyPlan => studyPlan.Name)
            .IsUnique();

        builder.Property(studyPlan => studyPlan.Coefficient)
            .HasColumnName("coefficient")
            .HasColumnType("numeric(3,2)")
            .HasPrecision(3, 2)
            .HasConversion(
                coefficient => coefficient.Value,
                value => new StudyTime.Domain.ValueObjects.Coefficient(value))
            .IsRequired();

        builder.Property(studyPlan => studyPlan.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(20)")
            .HasConversion(
                status => status == StudyPlanStatus.Active ? "active" : "inactive",
                value => value == "active"
                    ? StudyPlanStatus.Active
                    : StudyPlanStatus.Inactive)
            .IsRequired();
    }
}