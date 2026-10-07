using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyRecordConfiguration : IEntityTypeConfiguration<StudyRecord>
{
    public void Configure(EntityTypeBuilder<StudyRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tb_study_record", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_tb_study_record_minutes_positive",
                "minutes > 0");
        });

        builder.HasKey(studyRecord => studyRecord.Id);

        builder.Property(studyRecord => studyRecord.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(studyRecord => studyRecord.Date)
            .HasColumnName("date")
            .HasColumnType("date")
            .HasDefaultValueSql(
                "((NOW() AT TIME ZONE 'America/Sao_Paulo')::date)")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(studyRecord => studyRecord.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(studyRecord => studyRecord.Minutes)
            .HasColumnName("minutes")
            .HasColumnType("integer")
            .HasConversion(
                minutes => minutes.Value,
                value => new Minutes(value))
            .IsRequired();

        builder.Property(studyRecord => studyRecord.StudyAreaWeekId)
            .HasColumnName("study_area_week_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne<StudyAreaWeek>()
            .WithMany()
            .HasForeignKey(studyRecord => studyRecord.StudyAreaWeekId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}