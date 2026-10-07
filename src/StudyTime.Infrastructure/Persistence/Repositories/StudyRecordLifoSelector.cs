using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyRecordLifoSelector : IStudyRecordLifoSelector
{
    private readonly StudyTimeDbContext _dbContext;

    public StudyRecordLifoSelector(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<StudyRecord?> SelectLastEligibleAsync(Guid studyAreaWeekId, CancellationToken cancellationToken = default)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            throw new ArgumentException("StudyAreaWeekId cannot be empty.", nameof(studyAreaWeekId));
        }

        return _dbContext.StudyRecords
            .Where(studyRecord => studyRecord.StudyAreaWeekId == studyAreaWeekId)
            .OrderByDescending(studyRecord => studyRecord.CreatedAt)
            .ThenByDescending(studyRecord => studyRecord.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}