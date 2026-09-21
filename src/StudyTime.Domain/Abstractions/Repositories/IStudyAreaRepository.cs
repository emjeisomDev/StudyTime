using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyAreaRepository
{
    public Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken token);
    public Task AddAsync(StudyArea entity, CancellationToken token);
    public void Update(StudyArea entity);
    public void Remove(StudyArea entity);
}
