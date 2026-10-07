using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyAreaRepository : IStudyAreaRepository
{
    private readonly StudyTimeDbContext _dbContext;

    public StudyAreaRepository(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyAreas.AsNoTracking()
            .FirstOrDefaultAsync(studyArea => studyArea.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyArea>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyAreas
            .AsNoTracking()
            .OrderBy(studyArea => studyArea.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StudyArea studyArea, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(studyArea);
        await _dbContext.StudyAreas.AddAsync(studyArea, cancellationToken);
    }

    public void Update(StudyArea studyArea)
    {
        ArgumentNullException.ThrowIfNull(studyArea);
        _dbContext.StudyAreas.Update(studyArea);
    }

    public void Delete(StudyArea studyArea)
    {
        ArgumentNullException.ThrowIfNull(studyArea);
        _dbContext.StudyAreas.Remove(studyArea);
    }
}