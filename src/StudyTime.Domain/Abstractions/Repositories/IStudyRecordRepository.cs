using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyRecordRepository
{
    public Task<StudyRecord?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StudyRecord>> GetByStudyAreaWeekIdAsync
        (Guid studyAreaWeekId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StudyRecord>> GetByStudyAreaWeekIdOrderedByCreatedAtDescendingAsync
        (Guid studyAreaWeekId, CancellationToken cancellationToken = default);

    public Task AddAsync(StudyRecord studyRecord, CancellationToken cancellationToken = default);

    public void Delete(StudyRecord studyRecord);
}