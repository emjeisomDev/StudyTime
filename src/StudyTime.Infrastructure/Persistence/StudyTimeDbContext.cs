using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace StudyTime.Infrastructure.Persistence;

public sealed class StudyTimeDbContext : DbContext
{
    public StudyTimeDbContext(DbContextOptions<StudyTimeDbContext> options) : base(options)
    { }

    public DbSet<StudyArea> StudyAreas => Set<StudyArea>();
    public DbSet<StudyAreaWeek> StudyAreaWeeks => Set<StudyAreaWeek>();
    public DbSet<StudyAreaWeekAssessment> StudyAreaWeekAssessments => Set<StudyAreaWeekAssessment>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudyRecord> StudyRecords => Set<StudyRecord>();
    public DbSet<WeeklyAssessment> WeeklyAssessments => Set<WeeklyAssessment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudyTimeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}