using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyPlanRepository
{
    public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken token);
    public Task AddAsync(StudyPlan entity, CancellationToken token);
    public void Update(StudyPlan entity);
    public void Remove(StudyPlan entity);
}
