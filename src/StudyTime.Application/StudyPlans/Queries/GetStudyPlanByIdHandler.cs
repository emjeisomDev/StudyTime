using MediatR;

using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;

namespace StudyTime.Application.StudyPlans.Queries;

public sealed class GetStudyPlanByIdHandler
    : IRequestHandler<GetStudyPlanByIdQuery, StudyPlanDto?>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlanByIdHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<StudyPlanDto?> Handle(
        GetStudyPlanByIdQuery request,
        CancellationToken ct)
    {
        StudyPlan? plan = await _repo.GetByIdAsync(request.Id, ct);

        if (plan is null)
        {
            return null;
        }

        return new StudyPlanDto(
            plan.Id,
            plan.Name,
            plan.Coefficient,
            plan.Status);
    }
}