using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyRecordRepository : IStudyRecordRepository
{
    private readonly StudyTimeDbContext _dbContext;

    public StudyRecordRepository(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<StudyRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(studyRecord => studyRecord.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyRecord>> GetByStudyAreaWeekIdAsync(
        Guid studyAreaWeekId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyRecords
            .AsNoTracking()
            .Where(studyRecord => studyRecord.StudyAreaWeekId == studyAreaWeekId)
            .OrderBy(studyRecord => studyRecord.Date)
            .ThenBy(studyRecord => studyRecord.CreatedAt)
            .ThenBy(studyRecord => studyRecord.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyRecord>> GetByStudyAreaWeekIdOrderedByCreatedAtDescendingAsync
        (Guid studyAreaWeekId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyRecords
            .AsNoTracking()
            .Where(studyRecord => studyRecord.StudyAreaWeekId == studyAreaWeekId)
            .OrderByDescending(studyRecord => studyRecord.CreatedAt)
            .ThenByDescending(studyRecord => studyRecord.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StudyRecord studyRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(studyRecord);
        await _dbContext.StudyRecords.AddAsync(studyRecord, cancellationToken);
    }

    public void Delete(StudyRecord studyRecord)
    {
        ArgumentNullException.ThrowIfNull(studyRecord);
        _dbContext.StudyRecords.Remove(studyRecord);
    }
}