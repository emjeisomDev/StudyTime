using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaConfiguration : IEntityTypeConfiguration<StudyArea>
{
    public void Configure(EntityTypeBuilder<StudyArea> builder)
    {
        builder.ToTable(
            "tb_study_area",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_area_std_week_study_time_positive",
                    "std_week_study_time > 0");
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

        builder.Property(entity => entity.StdWeekStudyTime)
            .HasColumnName("std_week_study_time")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasIndex(entity => entity.Name)
            .IsUnique()
            .HasDatabaseName("ux_tb_study_area_name");
    }
}