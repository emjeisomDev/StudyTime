using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence;

public sealed class StudyTimeDbContext(DbContextOptions<StudyTimeDbContext> options) : DbContext(options)
{
    public DbSet<StudyArea> StudyAreas => Set<StudyArea>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudyAreaWeek> StudyAreaWeeks => Set<StudyAreaWeek>();
    public DbSet<StudyAreaWeekAssessment> StudyAreaWeekAssessments => Set<StudyAreaWeekAssessment>();
    public DbSet<WeeklyAssessment> WeeklyAssessments => Set<WeeklyAssessment>();
    public DbSet<StudyRecord> StudyRecords => Set<StudyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudyTimeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
