using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace StudyTime.Application.StudyPlans;

public sealed class StudyPlanService(IStudyPlanRepository repository) : IStudyPlanService
{
    public async Task<StudyPlanResponse> CreateAsync(CreateStudyPlanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyPlan = StudyPlan.Create(request.Name ?? string.Empty, request.Coefficient);

        repository.Add(studyPlan);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(studyPlan);
    }

    public async Task<IReadOnlyList<StudyPlanResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var studyPlans = await repository.ListAsync(cancellationToken);
        return studyPlans.Select(Map).ToArray();
    }

    public async Task<StudyPlanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var studyPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Study plan '{id}' was not found.");

        return Map(studyPlan);
    }

    public async Task<StudyPlanResponse> UpdateAsync(Guid id, UpdateStudyPlanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Study plan '{id}' was not found.");

        var candidate = StudyPlan.Create(id, request.Name ?? string.Empty, request.Coefficient, studyPlan.Status);

        studyPlan.Rename(candidate.Name);
        studyPlan.ChangeCoefficient(candidate.Coefficient);

        await repository.SaveChangesAsync(cancellationToken);

        return Map(studyPlan);
    }

    public async Task<StudyPlanResponse> ChangeStatusAsync(Guid id, ChangeStudyPlanStatusRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.Status))
            throw new ArgumentOutOfRangeException(nameof(request), "The study plan status is invalid.");

        var studyPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Study plan '{id}' was not found.");

        switch (request.Status)
        {
            case StudyPlanStatus.Active:
                studyPlan.Activate();
                break;
            case StudyPlanStatus.Inactive:
                studyPlan.Deactivate();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), "The study plan status is invalid.");
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Map(studyPlan);
    }

    private static StudyPlanResponse Map(StudyPlan studyPlan)
        => new(studyPlan.Id, studyPlan.Name, studyPlan.Coefficient, studyPlan.Status);
}