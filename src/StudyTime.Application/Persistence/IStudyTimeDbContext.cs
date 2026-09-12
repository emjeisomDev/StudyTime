using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Entities;

namespace StudyTime.Application.Persistence;

public interface IStudyTimeDbContext
{
    public DbSet<StudyArea> StudyAreas { get; }

    public DbSet<StudyPlan> StudyPlans { get; }

    public DbSet<WeeklyAssessment> WeeklyAssessments { get; }

    public DbSet<StudyAreaWeek> StudyAreaWeeks { get; }

    public DbSet<StudyAreaWeekAssessment> StudyAreaWeekAssessments { get; }

    public DbSet<StudyRecord> StudyRecords { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}