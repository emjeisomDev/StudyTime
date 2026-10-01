using MediatR;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyPlans.Commands;

public sealed class CreateStudyPlanHandler(
    IStudyPlanRepository studyPlanRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStudyPlanCommand, StudyPlanDto>
{
    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository ?? throw new ArgumentNullException(
            nameof(studyPlanRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(
            nameof(unitOfWork));

    public async Task<StudyPlanDto> Handle(
        CreateStudyPlanCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var coefficient = new Coefficient(request.Coefficient);

        var studyPlan = new StudyPlan(
            request.Name,
            coefficient);

        await _studyPlanRepository.AddAsync(
            studyPlan,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudyPlanDto(
            studyPlan.Id,
            studyPlan.Name,
            studyPlan.Coefficient.Value,
            studyPlan.Status.ToString());
    }
}