using Microsoft.EntityFrameworkCore;
using StudyTime.Application.Persistence;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence;

public class StudyTimeDbContext : DbContext, IStudyTimeDbContext
{
    public DbSet<StudyArea> StudyAreas => Set<StudyArea>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<WeeklyAssessment> WeeklyAssessments => Set<WeeklyAssessment>();
    public DbSet<StudyAreaWeek> StudyAreaWeeks => Set<StudyAreaWeek>();
    public DbSet<StudyAreaWeekAssessment> StudyAreaWeekAssessments => Set<StudyAreaWeekAssessment>();
    public DbSet<StudyRecord> StudyRecords => Set<StudyRecord>();
    
    public StudyTimeDbContext(DbContextOptions<StudyTimeDbContext> options): base(options)
    {  }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudyTimeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

}
