using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyPlanRepository
{
    public Task<StudyPlan?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StudyPlan>> GetAllAsync
        (CancellationToken cancellationToken = default);

    public Task AddAsync
        (StudyPlan studyPlan, CancellationToken cancellationToken = default);

    public void Update(StudyPlan studyPlan);

    public void Delete(StudyPlan studyPlan);
}