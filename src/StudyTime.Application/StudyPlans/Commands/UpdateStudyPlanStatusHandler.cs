using MediatR;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed class UpdateStudyPlanStatusHandler(
    IStudyPlanRepository studyPlanRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStudyPlanStatusCommand>
{
    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository ?? throw new ArgumentNullException(
            nameof(studyPlanRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(
            nameof(unitOfWork));

    public async Task Handle(
        UpdateStudyPlanStatusCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyPlan = await _studyPlanRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (studyPlan is null)
        {
            throw new KeyNotFoundException(
                $"StudyPlan with id '{request.Id}' was not found.");
        }

        studyPlan.ChangeStatus(request.Status);

        _studyPlanRepository.Update(studyPlan);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}