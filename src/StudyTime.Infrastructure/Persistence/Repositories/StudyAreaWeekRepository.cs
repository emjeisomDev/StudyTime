using StudyTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyAreaWeekRepository : IStudyAreaWeekRepository
{
    private readonly StudyTimeDbContext _dbContext;

    public StudyAreaWeekRepository(StudyTimeDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<StudyAreaWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyAreaWeeks
            .AsNoTracking()
            .FirstOrDefaultAsync(studyAreaWeek => studyAreaWeek.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyAreaWeek>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyAreaWeeks
            .AsNoTracking()
            .OrderBy(studyAreaWeek => studyAreaWeek.WeekStartDate)
            .ThenBy(studyAreaWeek => studyAreaWeek.StudyAreaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyAreaWeek>> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudyAreaWeeks
            .AsNoTracking()
            .Where(studyAreaWeek => studyAreaWeek.WeekStartDate == weekStartDate)
            .OrderBy(studyAreaWeek => studyAreaWeek.StudyAreaId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyAreaWeeks
            .AnyAsync(studyAreaWeek => studyAreaWeek.WeekStartDate == weekStartDate, cancellationToken);
    }

    public Task<bool> ExistsForStudyAreaInWeekAsync(Guid studyAreaId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        return _dbContext.StudyAreaWeeks
            .AnyAsync(studyAreaWeek =>
                    studyAreaWeek.StudyAreaId == studyAreaId &&
                    studyAreaWeek.WeekStartDate == weekStartDate,
                cancellationToken);
    }

    public async Task AddAsync(StudyAreaWeek studyAreaWeek, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(studyAreaWeek);
        await _dbContext.StudyAreaWeeks.AddAsync(studyAreaWeek, cancellationToken);
    }

    public void Update(StudyAreaWeek studyAreaWeek)
    {
        ArgumentNullException.ThrowIfNull(studyAreaWeek);
        _dbContext.StudyAreaWeeks.Update(studyAreaWeek);
    }

    public void Delete(StudyAreaWeek studyAreaWeek)
    {
        ArgumentNullException.ThrowIfNull(studyAreaWeek);
        _dbContext.StudyAreaWeeks.Remove(studyAreaWeek);
    }
}