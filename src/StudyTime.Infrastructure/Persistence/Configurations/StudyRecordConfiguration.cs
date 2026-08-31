using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyRecordConfiguration : IEntityTypeConfiguration<StudyRecord>
{
    public void Configure(EntityTypeBuilder<StudyRecord> builder)
    {
        builder.ToTable("tb_study_record");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

        builder.Property(x => x.Date)
                .HasColumnName("date")
                .HasColumnType("date")
                .HasDefaultValueSql("((NOW() AT TIME ZONE 'America/Sao_Paulo')::date)")
                .ValueGeneratedOnAdd()
                .IsRequired();

        builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd().IsRequired();

        builder.Property(x => x.Minutes)
                .HasColumnName("minutes")
                .HasColumnType("integer")
                .IsRequired();

        builder.Property(x => x.StudyAreaWeekId)
                .HasColumnName("study_area_week_id")
                .HasColumnType("uuid")
                .IsRequired();

        builder.HasOne<StudyAreaWeek>()
                .WithMany()
                .HasForeignKey(x => x.StudyAreaWeekId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_study_record_study_area_week");
                
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_study_record_minutes_positive", "minutes > 0"));
    }
}
