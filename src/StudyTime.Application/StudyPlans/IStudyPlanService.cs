namespace StudyTime.Application.StudyPlans;

public interface IStudyPlanService
{
    Task<StudyPlanResponse> CreateAsync(CreateStudyPlanRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudyPlanResponse>> ListAsync(CancellationToken cancellationToken);
    Task<StudyPlanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StudyPlanResponse> UpdateAsync(Guid id, UpdateStudyPlanRequest request, CancellationToken cancellationToken);
    Task<StudyPlanResponse> ChangeStatusAsync(Guid id, ChangeStudyPlanStatusRequest request, CancellationToken cancellationToken);
}