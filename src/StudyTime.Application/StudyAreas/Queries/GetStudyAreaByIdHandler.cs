using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreas.Queries;

public sealed class GetStudyAreaByIdHandler(IStudyAreaRepository studyAreaRepository)
    : IRequestHandler<GetStudyAreaByIdQuery, StudyAreaDto>
{
    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository ?? throw new ArgumentNullException(
            nameof(studyAreaRepository));

    public async Task<StudyAreaDto> Handle(
        GetStudyAreaByIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "StudyArea id cannot be empty.",
                nameof(request.Id));
        }

        var studyArea = await _studyAreaRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (studyArea is null)
        {
            throw new KeyNotFoundException(
                $"StudyArea with id '{request.Id}' was not found.");
        }

        return new StudyAreaDto(
            studyArea.Id,
            studyArea.Name,
            studyArea.StdWeekStudyTime.Value);
    }
}