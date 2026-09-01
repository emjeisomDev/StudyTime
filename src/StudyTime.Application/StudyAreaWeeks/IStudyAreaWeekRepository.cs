using StudyTime.Domain.Entities;

namespace StudyTime.Application.StudyAreaWeeks;

public interface IStudyAreaWeekRepository
{
    Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken);
    Task<bool> ExistsByAreaAndWeekAsync(Guid studyAreaId, DateOnly weekStartDate, CancellationToken cancellationToken);
    Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(int year, int weekNumber, CancellationToken cancellationToken);
    void Add(StudyAreaWeek studyAreaWeek);
    void AddWeeklyAssessment(WeeklyAssessment weeklyAssessment);
}