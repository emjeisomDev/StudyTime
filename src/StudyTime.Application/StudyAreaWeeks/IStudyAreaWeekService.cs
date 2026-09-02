namespace StudyTime.Application.StudyAreaWeeks;

public interface IStudyAreaWeekService
{
    Task<StudyAreaWeekResponse> CreateAsync(CreateStudyAreaWeekRequest request, CancellationToken cancellationToken);
    Task<StudyAreaWeekBatchResponse> CreateBatchAsync(CreateStudyAreaWeekBatchRequest request, CancellationToken cancellationToken);
}
