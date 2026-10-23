using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreas.Queries;

public sealed record GetStudyAreasQuery : IRequest<IReadOnlyList<StudyAreaDto>>;

