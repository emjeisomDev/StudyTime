using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed class CreateStudyPlanHandler : ICommandHandler<CreateStudyPlanCommand, StudyPlanDto>
{
    private readonly IStudyPlanRepository _repo;

    public CreateStudyPlanHandler(IStudyPlanRepository repo) => _repo = repo;

    public async Task<StudyPlanDto> Handle(CreateStudyPlanCommand request, CancellationToken ct)
    {
        StudyPlan plan = new(Guid.NewGuid(), request.Name, request.Coefficient, request.Status);
        await _repo.AddAsync(plan, ct);
        return new StudyPlanDto(plan.Id, plan.Name, plan.Coefficient, plan.Status);
    }
}