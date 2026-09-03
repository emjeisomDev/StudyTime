namespace StudyTime.Application.StudyAreaWeeks;

public interface IStudyAreaWeekService
{
    Task<StudyAreaWeekResponse> CreateAsync
        (CreateStudyAreaWeekRequest request, CancellationToken cancellationToken);
    Task<StudyAreaWeekBatchResponse> CreateBatchAsync
        (CreateStudyAreaWeekBatchRequest request, CancellationToken cancellationToken);
    Task<StudyAreaWeekAssessmentResponse?> GetAssessmentAsync
        (Guid studyAreaWeekId, CancellationToken cancellationToken);
    Task<StudyAreaWeekResponse?> UpdateAsync
        (Guid studyAreaWeekId, UpdateStudyAreaWeekRequest request, CancellationToken cancellationToken);
}
