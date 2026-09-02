using Microsoft.EntityFrameworkCore;
using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyAreaWeekRepository(StudyTimeDbContext dbContext) : IStudyAreaWeekRepository
{

    public Task<StudyAreaWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.StudyAreaWeeks
            .Include(x => x.Assessment)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken)
    {
        return await dbContext.StudyAreaWeeks
            .Include(x => x.Assessment)
            .Where(x => x.WeekStartDate == weekStartDate)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByAreaAndWeekAsync(Guid studyAreaId, DateOnly weekStartDate, CancellationToken cancellationToken)
    {
        return dbContext.StudyAreaWeeks.AnyAsync(
            x => x.StudyAreaId == studyAreaId && x.WeekStartDate == weekStartDate,
            cancellationToken);
    }

    public Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(int year, int weekNumber, CancellationToken cancellationToken)
    {
        return dbContext.WeeklyAssessments
            .SingleOrDefaultAsync(
                x => x.Year == year && x.WeekNumber == weekNumber,
                cancellationToken);
    }

    public void Add(StudyAreaWeek studyAreaWeek)
    {
        dbContext.StudyAreaWeeks.Add(studyAreaWeek);
    }

    public void AddWeeklyAssessment(WeeklyAssessment weeklyAssessment)
    {
        dbContext.WeeklyAssessments.Add(weeklyAssessment);
    }
}