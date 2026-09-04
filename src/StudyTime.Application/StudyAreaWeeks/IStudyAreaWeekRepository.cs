using StudyTime.Domain.Entities;

namespace StudyTime.Application.StudyAreaWeeks;

public interface IStudyAreaWeekRepository
{
    Task<StudyAreaWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken);
    Task<bool> ExistsByAreaAndWeekAsync(Guid studyAreaId, DateOnly weekStartDate, CancellationToken cancellationToken);
    Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(int year, int weekNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudyRecord>> ListStudyRecordsByWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken);
    void Add(StudyAreaWeek studyAreaWeek);
    void AddWeeklyAssessment(WeeklyAssessment weeklyAssessment);
}