using MediatR;
using StudyTime.Application.Dtos;

namespace StudyTime.Application.WeeklyAssessments.Queries;

public sealed record GetPreviousWeekSummaryQuery : IRequest<PreviousWeekSummaryDto>;