using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed class ChangeStudyPlanStatusHandler
    : ICommandHandler<ChangeStudyPlanStatusCommand, StudyPlanDto>
{
    private readonly IStudyPlanRepository _repo;

    public ChangeStudyPlanStatusHandler(IStudyPlanRepository repo) => _repo = repo;

    public async Task<StudyPlanDto> Handle(
        ChangeStudyPlanStatusCommand request,
        CancellationToken ct)
    {
        StudyPlan? existing = await _repo.GetByIdAsync(request.PlanId, ct);

        if (existing is null)
        {
            throw new DomainValidationException(
                "StudyPlan not found.");
        }

        switch (request.NewStatus)
        {
            case StudyPlanStatus.Active:
                existing.Activate();
                break;

            case StudyPlanStatus.Inactive:
                existing.Deactivate();
                break;

            default:
                throw new DomainValidationException("Study plan status must be Active or Inactive.");
        }

        _repo.Update(existing);

        return new StudyPlanDto(
            existing.Id,
            existing.Name,
            existing.Coefficient,
            existing.Status);
    }
}