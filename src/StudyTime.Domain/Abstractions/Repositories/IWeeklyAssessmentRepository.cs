using StudyTime.Domain.Entities;

namespace StudyTime.Domain.Abstractions.Repositories;

public interface IWeeklyAssessmentRepository
{
    public Task<WeeklyAssessment?> GetByIdAsync
        (Guid id, CancellationToken cancellationToken = default);

    public Task<WeeklyAssessment?> GetByYearAndWeekNumberAsync
        (int year, int weekNumber, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<WeeklyAssessment>> GetAllAsync
        (CancellationToken cancellationToken = default);

    public Task AddAsync
        (WeeklyAssessment assessment, CancellationToken cancellationToken = default);

    public void Update(WeeklyAssessment assessment);

    public void Delete(WeeklyAssessment assessment);
}