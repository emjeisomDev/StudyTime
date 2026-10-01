using MediatR;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed class CreateStudyAreaHandler(
    IStudyAreaRepository studyAreaRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStudyAreaCommand, StudyAreaDto>
{
    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository ?? throw new ArgumentNullException(
            nameof(studyAreaRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<StudyAreaDto> Handle(
        CreateStudyAreaCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyArea = new StudyArea(
            request.Name,
            new Minutes(request.StdWeekStudyTime));

        await _studyAreaRepository.AddAsync(
            studyArea,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudyAreaDto(
            studyArea.Id,
            studyArea.Name,
            studyArea.StdWeekStudyTime.Value);
    }
}