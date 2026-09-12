using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable(
            "tb_study_plan",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_plan_coefficient_positive",
                    "coefficient > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_plan_status_valid",
                    "status IN ('active', 'inactive')");
            });

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(entity => entity.Coefficient)
            .HasColumnName("coefficient")
            .HasColumnType("numeric(3,2)")
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(8)")
            .HasConversion(
                status => status.ToStorageValue(),
                value => StudyPlanStatusExtensions.Parse(value))
            .IsRequired();
    }
}