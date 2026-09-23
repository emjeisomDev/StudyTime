using StudyTime.Application.Abstractions.Messaging;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.StudyPlans.Queries;

public sealed record GetStudyPlanByIdQuery(Guid Id) : IQuery<StudyPlanDto?>;