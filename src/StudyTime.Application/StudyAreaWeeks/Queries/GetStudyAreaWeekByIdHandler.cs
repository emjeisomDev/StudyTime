using MediatR;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreaWeeks.Queries;

public sealed class GetStudyAreaWeekByIdHandler(
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IIsoWeekCalendar isoWeekCalendar)
    : IRequestHandler<GetStudyAreaWeekByIdQuery, StudyAreaWeekDto>
{
    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository
        ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository
        _studyAreaWeekAssessmentRepository =
            studyAreaWeekAssessmentRepository
            ?? throw new ArgumentNullException(
                nameof(studyAreaWeekAssessmentRepository));

    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository
        ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    public async Task<StudyAreaWeekDto> Handle(GetStudyAreaWeekByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(request.Id));
        }

        var studyAreaWeek =
            await _studyAreaWeekRepository.GetByIdAsync(request.Id, cancellationToken);

        if (studyAreaWeek is null)
        {
            throw new KeyNotFoundException(
                $"StudyAreaWeek '{request.Id}' was not found.");
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
                @"The individual assessment does not 
                belong to the requested StudyAreaWeek.");
        }

        var isoWeek = _isoWeekCalendar.GetIsoWeek(studyAreaWeek.WeekStartDate);

        var weeklyAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(isoWeek.Year, isoWeek.WeekNumber, cancellationToken);

        if (weeklyAssessment is null || weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException(
                @"The weekly assessment associated with the 
                configuration does not match its ISO week.");
        }

        return new StudyAreaWeekDto(
            studyAreaWeek.Id,
            studyAreaWeek.WeekStartDate,
            studyAreaWeek.StudyAreaId,
            studyAreaWeek.StudyPlanId,
            studyAreaWeek.WeeklyAssessmentId,
            individualAssessment.WeekIndividualGoal,
            individualAssessment.MinutesStudied,
            GoalAchievedEvaluator.EvaluateIndividual(individualAssessment));
    }
}