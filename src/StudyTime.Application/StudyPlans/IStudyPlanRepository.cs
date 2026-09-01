using StudyTime.Domain.Entities;

namespace StudyTime.Application.StudyPlans;

public interface IStudyPlanRepository
{
    Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudyPlan>> ListAsync(CancellationToken cancellationToken);
    void Add(StudyPlan studyPlan);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}