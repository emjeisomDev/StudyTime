using StudyTime.Domain.Entities;

namespace StudyTime.Application.StudyAreas;

public interface IStudyAreaRepository
{
    Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudyArea>> ListAsync(CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken);
    Task<bool> HasDependenciesAsync(Guid studyAreaId, CancellationToken cancellationToken);
    void Add(StudyArea studyArea);
    void Remove(StudyArea studyArea);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}