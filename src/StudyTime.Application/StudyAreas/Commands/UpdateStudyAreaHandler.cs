using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreas.Commands;

public sealed class UpdateStudyAreaHandler : ICommandHandler<UpdateStudyAreaCommand, StudyAreaDto>
{
    private readonly IStudyAreaRepository _repo;

    public UpdateStudyAreaHandler(IStudyAreaRepository repo) => _repo = repo;

    public async Task<StudyAreaDto> Handle(UpdateStudyAreaCommand request, CancellationToken token)
    {
        StudyArea? existing = await _repo.GetByIdAsync(request.Id, token);

        if (existing is null)
        {
            throw new DomainValidationException("StudyArea not found.");
        }

        StudyArea updated = new(existing.Id, request.Name, request.StdWeekStudyTime);
        _repo.Update(updated);
        return new StudyAreaDto(updated.Id, updated.Name, updated.StdWeekStudyTime);
    }
}