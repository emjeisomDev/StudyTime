using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;

namespace StudyTime.Application.StudyAreas.Queries;

public sealed record GetStudyAreaByIdQuery(Guid Id) : IQuery<StudyAreaDto?>;