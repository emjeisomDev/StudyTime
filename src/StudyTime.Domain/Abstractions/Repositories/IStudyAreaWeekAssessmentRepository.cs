using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IStudyAreaWeekAssessmentRepository
{
    public Task<StudyAreaWeekAssessment?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default);

    public Task<StudyAreaWeekAssessment?> GetByStudyAreaWeekIdAsync
        (Guid studyAreaWeekId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StudyAreaWeekAssessment>> GetAllAsync
        (CancellationToken cancellationToken = default);

    public Task AddAsync
        (StudyAreaWeekAssessment assessment, CancellationToken cancellationToken = default);

    public void Update(StudyAreaWeekAssessment assessment);

    public void Delete(StudyAreaWeekAssessment assessment);
}