using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed class CreateStudyAreaHandler : ICommandHandler<CreateStudyAreaCommand, StudyAreaDto>
{
    private readonly IStudyAreaRepository _repo;

    public CreateStudyAreaHandler(IStudyAreaRepository repo) => _repo = repo;

    public async Task<StudyAreaDto> Handle(CreateStudyAreaCommand request, CancellationToken token)
    {
        StudyArea area = new(Guid.NewGuid(), request.Name, request.StdWeekStudyTime);
        await _repo.AddAsync(area, token);
        return new StudyAreaDto(area.Id, area.Name, area.StdWeekStudyTime);
    }
}