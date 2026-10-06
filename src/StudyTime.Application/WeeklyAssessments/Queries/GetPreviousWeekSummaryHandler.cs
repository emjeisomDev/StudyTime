using MediatR;
using AutoMapper;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.WeeklyAssessments.Queries;

public sealed class GetPreviousWeekSummaryHandler(
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IMapper mapper)
    : IRequestHandler<GetPreviousWeekSummaryQuery, PreviousWeekSummaryDto>
{
    private const string IndividualAssessmentsContextKey = "IndividualAssessments";

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider
        ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository
        ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository
        ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository
        _studyAreaWeekAssessmentRepository =
            studyAreaWeekAssessmentRepository
            ?? throw new ArgumentNullException(
                nameof(studyAreaWeekAssessmentRepository));

    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository
        ?? throw new ArgumentNullException(nameof(studyAreaRepository));

    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository
        ?? throw new ArgumentNullException(nameof(studyPlanRepository));

    private readonly IMapper _mapper =
        mapper
        ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<PreviousWeekSummaryDto> Handle(
        GetPreviousWeekSummaryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentWeekStart = DateOnly.FromDateTime(
            _currentWeekProvider.GetCurrentWeek().Start);

        var previousWeekStart = currentWeekStart.AddDays(-7);

        if (previousWeekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException(
                "The calculated previous week must start on a Monday.");
        }

        var previousIsoWeek =
            _isoWeekCalendar.GetIsoWeek(previousWeekStart);

        var weeklyAssessment =
            await _weeklyAssessmentRepository
                .GetByYearAndWeekNumberAsync(
                    previousIsoWeek.Year,
                    previousIsoWeek.WeekNumber,
                    cancellationToken);

        if (weeklyAssessment is null)
        {
            throw new KeyNotFoundException(
                $"WeeklyAssessment for ISO week " +
                $"{previousIsoWeek.Year}-W{previousIsoWeek.WeekNumber:D2} " +
                "was not found.");
        }

        if (weeklyAssessment.Year != previousIsoWeek.Year ||
            weeklyAssessment.WeekNumber != previousIsoWeek.WeekNumber)
        {
            throw new DomainException(
                "The WeeklyAssessment does not match the previous ISO week.");
        }

        var studyAreaWeeks =
            await _studyAreaWeekRepository.GetByWeekStartDateAsync(
                previousWeekStart,
                cancellationToken);

        if (studyAreaWeeks.Count == 0)
        {
            throw new DomainException(
                $"No StudyAreaWeek configuration was found for ISO week " +
                $"{previousIsoWeek.Year}-W{previousIsoWeek.WeekNumber:D2}.");
        }

        var items =
            new List<PreviousWeekSummaryItemDto>(studyAreaWeeks.Count);

        var individualAssessments =
            new List<StudyTime.Domain.Entities.StudyAreaWeekAssessment>(
                studyAreaWeeks.Count);

        foreach (var studyAreaWeek in studyAreaWeeks)
        {
            if (studyAreaWeek.WeeklyAssessmentId != weeklyAssessment.Id)
            {
                throw new DomainException(
                    "A StudyAreaWeek is associated with a WeeklyAssessment " +
                    "belonging to another week.");
            }

            var studyArea =
                await _studyAreaRepository.GetByIdAsync(
                    studyAreaWeek.StudyAreaId,
                    cancellationToken);

            if (studyArea is null)
            {
                throw new KeyNotFoundException(
                    $"StudyArea '{studyAreaWeek.StudyAreaId}' was not found.");
            }

            var studyPlan =
                await _studyPlanRepository.GetByIdAsync(
                    studyAreaWeek.StudyPlanId,
                    cancellationToken);

            if (studyPlan is null)
            {
                throw new KeyNotFoundException(
                    $"StudyPlan '{studyAreaWeek.StudyPlanId}' was not found.");
            }

            var individualAssessment =
                await _studyAreaWeekAssessmentRepository
                    .GetByStudyAreaWeekIdAsync(
                        studyAreaWeek.Id,
                        cancellationToken);

            if (individualAssessment is null)
            {
                throw new DomainException(
                    "StudyAreaWeekAssessment was not found for " +
                    $"StudyAreaWeek '{studyAreaWeek.Id}'.");
            }

            if (individualAssessment.StudyAreaWeekId != studyAreaWeek.Id)
            {
                throw new DomainException(
                    "The StudyAreaWeekAssessment does not belong to " +
                    "its StudyAreaWeek.");
            }

            individualAssessments.Add(individualAssessment);

            items.Add(
                new PreviousWeekSummaryItemDto(
                    _mapper.Map<StudyAreaDto>(studyArea),
                    _mapper.Map<StudyPlanDto>(studyPlan),
                    individualAssessment.WeekIndividualGoal,
                    individualAssessment.MinutesStudied,
                    GoalAchievedEvaluator.EvaluateIndividual(
                        individualAssessment)));
        }

        var weeklyAssessmentDto =
            _mapper.Map<WeeklyAssessmentDto>(
                weeklyAssessment,
                options =>
                {
                    options.Items[IndividualAssessmentsContextKey] =
                        individualAssessments;
                });

        return new PreviousWeekSummaryDto(
            previousWeekStart,
            weeklyAssessmentDto,
            items);
    }
}