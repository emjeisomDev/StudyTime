using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyPlanRepository : IStudyPlanRepository
{
    private readonly StudyTimeDbContext _dbContext;

    public StudyPlanRepository(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(studyPlan => studyPlan.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyPlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyPlans
            .AsNoTracking()
            .OrderBy(studyPlan => studyPlan.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StudyPlan studyPlan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(studyPlan);
        await _dbContext.StudyPlans.AddAsync(studyPlan, cancellationToken);
    }

    public void Update(StudyPlan studyPlan)
    {
        ArgumentNullException.ThrowIfNull(studyPlan);
        _dbContext.StudyPlans.Update(studyPlan);
    }

    public void Delete(StudyPlan studyPlan)
    {
        ArgumentNullException.ThrowIfNull(studyPlan);
        _dbContext.StudyPlans.Remove(studyPlan);
    }
}