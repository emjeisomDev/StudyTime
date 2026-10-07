using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyAreaConfiguration : IEntityTypeConfiguration<StudyArea>
{
    public void Configure(EntityTypeBuilder<StudyArea> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tb_study_area", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_tb_study_area_std_week_study_time_positive",
                "std_week_study_time > 0");
        });

        builder.HasKey(studyArea => studyArea.Id);

        builder.Property(studyArea => studyArea.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(studyArea => studyArea.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(studyArea => studyArea.Name)
            .IsUnique();

        builder.Property(studyArea => studyArea.StdWeekStudyTime)
            .HasColumnName("std_week_study_time")
            .HasColumnType("integer")
            .HasConversion(
                minutes => minutes.Value,
                value => new StudyTime.Domain.ValueObjects.Minutes(value))
            .IsRequired();
    }
}