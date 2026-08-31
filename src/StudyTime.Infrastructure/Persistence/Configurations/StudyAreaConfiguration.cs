using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaConfiguration : IEntityTypeConfiguration<StudyArea>
{
    public void Configure(EntityTypeBuilder<StudyArea> builder)
    {
        builder.ToTable("tb_study_area");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

        builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasColumnType("varchar(80)")
                .IsRequired();

        builder.HasIndex(x => x.Name)
                .IsUnique()
                .HasDatabaseName("ux_tb_study_area_name");

        builder.Property(x => x.StdWeekStudyTime)
                .HasColumnName("std_week_study_time")
                .HasColumnType("integer").IsRequired();

        builder
            .ToTable(t => t.HasCheckConstraint("ck_tb_study_area_std_week_study_time_positive", "std_week_study_time > 0"));
    }
}