using StudyTime.Application.Dtos;
using StudyTime.Domain.Entities;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed class UpdateStudyAreaHandler(
    IStudyAreaRepository studyAreaRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateStudyAreaCommand, StudyAreaDto>
{
    private readonly IStudyAreaRepository _studyAreaRepository = studyAreaRepository ?? throw new ArgumentNullException(nameof(studyAreaRepository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<StudyAreaDto> Handle(UpdateStudyAreaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "StudyArea id cannot be empty.",
                nameof(request.Id));
        }

        if (request.Name is null && request.StdWeekStudyTime is null)
        {
            throw new ArgumentException(
                "At least one field must be provided for update.",
                nameof(request));
        }

        var existingStudyArea =
            await _studyAreaRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

        if (existingStudyArea is null)
        {
            throw new KeyNotFoundException(
                $"StudyArea with id '{request.Id}' was not found.");
        }

        var name = request.Name ?? existingStudyArea.Name;

        var weeklyStudyTime = request.StdWeekStudyTime.HasValue
            ? new Minutes(request.StdWeekStudyTime.Value)
            : existingStudyArea.StdWeekStudyTime;

        // A entidade permanece imutável: criamos uma nova instância.
        var updatedStudyArea = new StudyArea(
            existingStudyArea.Id,
            name,
            weeklyStudyTime);

        _studyAreaRepository.Update(updatedStudyArea);

        // Persiste as alterações dentro da transação aberta pelo behavior.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudyAreaDto(
            updatedStudyArea.Id,
            updatedStudyArea.Name,
            updatedStudyArea.StdWeekStudyTime.Value);
    }
}