namespace StudyTime.Application.StudyAreas;

public interface IStudyAreaService
{
    Task<StudyAreaResponse> CreateAsync(CreateStudyAreaRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudyAreaResponse>> ListAsync(CancellationToken cancellationToken);
    Task<StudyAreaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StudyAreaResponse> UpdateAsync(Guid id, UpdateStudyAreaRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
