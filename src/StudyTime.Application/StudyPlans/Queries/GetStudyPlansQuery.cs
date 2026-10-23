using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyPlans.Queries;

public sealed record GetStudyPlansQuery : IRequest<IReadOnlyList<StudyPlanDto>>;