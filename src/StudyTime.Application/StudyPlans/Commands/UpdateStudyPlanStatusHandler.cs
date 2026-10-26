using StudyTime.Application.Abstractions.Messaging;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed class UpdateStudyPlanStatusHandler(IStudyPlanRepository studyPlanRepository, IUnitOfWork unitOfWork)
     : ICommandHandler<UpdateStudyPlanStatusCommand, StudyPlanDto>
{
    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository ?? throw new ArgumentNullException(
            nameof(studyPlanRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(
            nameof(unitOfWork));

    public async Task<StudyPlanDto> Handle(
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

        return new StudyPlanDto(
            studyPlan.Id,
            studyPlan.Name,
            studyPlan.Coefficient.Value,
            studyPlan.Status.ToString().ToLowerInvariant());
    }
}