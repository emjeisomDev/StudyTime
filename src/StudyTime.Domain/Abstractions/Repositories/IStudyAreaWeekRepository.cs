using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyAreaWeekRepository
{
    public Task<StudyAreaWeek?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StudyAreaWeek>> GetAllAsync
        (
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StudyAreaWeek>> GetByWeekStartDateAsync
        (DateOnly weekStartDate, CancellationToken cancellationToken = default);

    public Task<bool> ExistsForWeekAsync
        (DateOnly weekStartDate, CancellationToken cancellationToken = default);

    public Task<bool> ExistsForStudyAreaInWeekAsync
        (Guid studyAreaId, DateOnly weekStartDate, CancellationToken cancellationToken = default);

    public Task AddAsync
        (StudyAreaWeek studyAreaWeek, CancellationToken cancellationToken = default);

    public void Update(StudyAreaWeek studyAreaWeek);

    public void Delete(StudyAreaWeek studyAreaWeek);
}