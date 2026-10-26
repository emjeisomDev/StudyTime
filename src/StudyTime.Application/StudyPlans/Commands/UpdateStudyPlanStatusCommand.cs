using StudyTime.Domain.Enums;
using StudyTime.Application.Dtos;
using StudyTime.Application.Abstractions.Messaging;


namespace StudyTime.Application.StudyPlans.Commands;

	public sealed record UpdateStudyPlanStatusCommand(
		Guid Id,
		StudyPlanStatus Status) : ICommand<StudyPlanDto>;