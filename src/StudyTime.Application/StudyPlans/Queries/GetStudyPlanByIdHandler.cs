using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyPlans.Queries;

public sealed class GetStudyPlanByIdHandler(IStudyPlanRepository studyPlanRepository)
    : IRequestHandler<GetStudyPlanByIdQuery, StudyPlanDto?>
{
    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository ?? throw new ArgumentNullException(
            nameof(studyPlanRepository));

    public async Task<StudyPlanDto?> Handle(GetStudyPlanByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyPlan = await _studyPlanRepository.GetByIdAsync(request.Id, cancellationToken);

        if (studyPlan is null)
        {
            return null;
        }

        return new StudyPlanDto(
            studyPlan.Id,
            studyPlan.Name,
            studyPlan.Coefficient.Value,
            studyPlan.Status.ToString());
    }
}