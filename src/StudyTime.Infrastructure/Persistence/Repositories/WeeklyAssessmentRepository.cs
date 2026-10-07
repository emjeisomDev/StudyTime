using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class WeeklyAssessmentRepository : IWeeklyAssessmentRepository
{
    private readonly StudyTimeDbContext _dbContext;

    public WeeklyAssessmentRepository(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<WeeklyAssessment?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.WeeklyAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(assessment => assessment.Id == id, cancellationToken);
    }

    public Task<WeeklyAssessment?> GetByYearAndWeekNumberAsync
        (int year, int weekNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.WeeklyAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(assessment => assessment.Year == year &&
                                                assessment.WeekNumber == weekNumber,
                                cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklyAssessment>> GetAllAsync
        (CancellationToken cancellationToken = default)
    {
        return await _dbContext.WeeklyAssessments
            .AsNoTracking()
            .OrderByDescending(assessment => assessment.Year)
            .ThenByDescending(assessment => assessment.WeekNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WeeklyAssessment assessment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        await _dbContext.WeeklyAssessments.AddAsync(assessment, cancellationToken);
    }

    public void Update(WeeklyAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        _dbContext.WeeklyAssessments.Update(assessment);
    }

    public void Delete(WeeklyAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        _dbContext.WeeklyAssessments.Remove(assessment);
    }
}