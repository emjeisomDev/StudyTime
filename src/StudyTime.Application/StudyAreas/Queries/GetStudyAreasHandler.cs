using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreas.Queries;

public sealed class GetStudyAreasHandler(IStudyAreaRepository studyAreaRepository)
    : IRequestHandler<GetStudyAreasQuery, IReadOnlyList<StudyAreaDto>>
{
    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository
        ?? throw new ArgumentNullException(nameof(studyAreaRepository));

    public async Task<IReadOnlyList<StudyAreaDto>> Handle(
        GetStudyAreasQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyAreas = await _studyAreaRepository.GetAllAsync(
            cancellationToken);

        return studyAreas
            .Select(studyArea => new StudyAreaDto(
                studyArea.Id,
                studyArea.Name,
                studyArea.StdWeekStudyTime.Value))
            .ToArray();
    }
}