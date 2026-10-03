using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyAreaWeeks.Queries;

public sealed record GetStudyAreaWeekByIdQuery(Guid Id) : IRequest<StudyAreaWeekDto>;