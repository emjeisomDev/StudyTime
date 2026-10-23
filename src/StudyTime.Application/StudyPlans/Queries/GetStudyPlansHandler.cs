using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyPlans.Queries;

public sealed class GetStudyPlansHandler(IStudyPlanRepository studyPlanRepository) : IRequestHandler<GetStudyPlansQuery, IReadOnlyList<StudyPlanDto>>
{
    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository
        ?? throw new ArgumentNullException(nameof(studyPlanRepository));

    public async Task<IReadOnlyList<StudyPlanDto>> Handle(
        GetStudyPlansQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyPlans = await _studyPlanRepository.GetAllAsync(
            cancellationToken);

        return studyPlans
            .Select(studyPlan => new StudyPlanDto(
                studyPlan.Id,
                studyPlan.Name,
                studyPlan.Coefficient.Value,
                studyPlan.Status.ToString().ToLowerInvariant()))
            .ToArray();
    }
}