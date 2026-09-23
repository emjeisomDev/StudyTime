using MediatR;

using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreas.Queries;

public sealed class GetStudyAreaByIdHandler : IRequestHandler<GetStudyAreaByIdQuery, StudyAreaDto?>
{
    private readonly IStudyAreaRepository _repo;

    public GetStudyAreaByIdHandler(IStudyAreaRepository repo) => _repo = repo;

    public async Task<StudyAreaDto?> Handle(GetStudyAreaByIdQuery request, CancellationToken token)
    {
        var area = await _repo.GetByIdAsync(request.Id, token);

        if (area is null)
        {
            return null;
        }
        
        return new StudyAreaDto(area.Id, area.Name, area.StdWeekStudyTime);
    }
}