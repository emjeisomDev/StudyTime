using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyAreaWeekAssessmentRepository : IStudyAreaWeekAssessmentRepository
{
    private readonly StudyTimeDbContext _dbContext;

    public StudyAreaWeekAssessmentRepository(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<StudyAreaWeekAssessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyAreaWeekAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(assessment => assessment.Id == id, cancellationToken);
    }

    public Task<StudyAreaWeekAssessment?> GetByStudyAreaWeekIdAsync(Guid studyAreaWeekId, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyAreaWeekAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(assessment =>
                assessment.StudyAreaWeekId == studyAreaWeekId, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyAreaWeekAssessment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyAreaWeekAssessments
            .AsNoTracking()
            .OrderBy(assessment => assessment.StudyAreaWeekId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StudyAreaWeekAssessment assessment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        await _dbContext.StudyAreaWeekAssessments.AddAsync(assessment, cancellationToken);
    }

    public void Update(StudyAreaWeekAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        _dbContext.StudyAreaWeekAssessments.Update(assessment);
    }

    public void Delete(StudyAreaWeekAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        _dbContext.StudyAreaWeekAssessments.Remove(assessment);
    }
}