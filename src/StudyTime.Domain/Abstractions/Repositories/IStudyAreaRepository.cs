using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyAreaRepository
{
    public Task<StudyArea?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<StudyArea>> GetAllAsync
        (CancellationToken cancellationToken = default);
    public Task AddAsync
            (StudyArea studyArea, CancellationToken cancellationToken = default);
    public void Update(StudyArea studyArea);
    public void Delete(StudyArea studyArea);
}