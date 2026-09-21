using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyAreaWeekRepository
{
    public Task<StudyAreaWeek?> GetByIdAsync(Guid id, CancellationToken token);
    public Task AddAsync(StudyAreaWeek entity, CancellationToken token);
    public void Update(StudyAreaWeek entity);
    public void Remove(StudyAreaWeek entity);
}
