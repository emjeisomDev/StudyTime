using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreas.Queries;

public sealed record GetStudyAreaByIdQuery(Guid Id) : IRequest<StudyAreaDto>;