namespace StudyTime.Application.StudyAreaWeeks;

public interface IStudyAreaWeekService
{
    Task<StudyAreaWeekResponse> CreateAsync(CreateStudyAreaWeekRequest request, CancellationToken cancellationToken);
}
