using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace StudyTime.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("tb_study_plan");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

        builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasColumnType("varchar(80)")
                .IsRequired();

        builder.Property(x => x.Coefficient)
                .HasColumnName("coefficient")
                .HasColumnType("numeric(3,2)")
                .IsRequired();

        builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasColumnType("varchar(20)")
                .HasConversion(v => v == StudyPlanStatus.Active ? "active" : "inactive", 
                               v => v == "active" ? StudyPlanStatus.Active : StudyPlanStatus.Inactive)
                .IsRequired();
                
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_study_plan_coefficient_positive", "coefficient > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_tb_study_plan_status", "status IN ('active','inactive')"));
    }
}