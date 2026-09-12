using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyRecordConfiguration
    : IEntityTypeConfiguration<StudyRecord>
{
    public void Configure(EntityTypeBuilder<StudyRecord> builder)
    {
        builder.ToTable(
            "tb_study_record",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tb_study_record_minutes_positive",
                    "minutes > 0");
            });

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entity => entity.Date)
            .HasColumnName("date")
            .HasColumnType("date")
            .HasDefaultValueSql(
                "(CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo')::date")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(entity => entity.Minutes)
            .HasColumnName("minutes")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(entity => entity.StudyAreaWeekId)
            .HasColumnName("study_area_week_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasOne(entity => entity.StudyAreaWeek)
            .WithMany(entity => entity.StudyRecords)
            .HasForeignKey(entity => entity.StudyAreaWeekId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_tb_study_record_study_area_week");

        builder.HasIndex(
                entity => new
                {
                    entity.StudyAreaWeekId,
                    entity.CreatedAt,
                    entity.Id
                })
            .HasDatabaseName("ix_tb_study_record_lifo");
    }
}