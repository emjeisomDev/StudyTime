using MediatR;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.WeeklyAssessments.Services;

namespace StudyTime.Application.StudyRecords.Commands;

public sealed class DeleteLastStudyRecordHandler(
    IIsoWeekCalendar isoWeekCalendar,
    ICurrentWeekProvider currentWeekProvider,
    IStudyRecordRepository studyRecordRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyRecordLifoSelector studyRecordLifoSelector,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IWeeklyAssessmentSynchronizer weeklyAssessmentSynchronizer,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository)
    : IRequestHandler<DeleteLastStudyRecordCommand, Unit>
{
    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository _studyAreaWeekAssessmentRepository =
        studyAreaWeekAssessmentRepository ?? throw new ArgumentNullException(nameof(studyAreaWeekAssessmentRepository));

    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    private readonly IStudyRecordLifoSelector _studyRecordLifoSelector =
        studyRecordLifoSelector ?? throw new ArgumentNullException(nameof(studyRecordLifoSelector));

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IWeeklyAssessmentSynchronizer _weeklyAssessmentSynchronizer =
        weeklyAssessmentSynchronizer ?? throw new ArgumentNullException(nameof(weeklyAssessmentSynchronizer));

    public async Task<Unit> Handle(
        DeleteLastStudyRecordCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.StudyAreaWeekId == Guid.Empty)
        {
            throw new ArgumentException("StudyAreaWeekId is required.", nameof(request.StudyAreaWeekId));
        }

        var studyAreaWeek = await _studyAreaWeekRepository.GetByIdAsync(
            request.StudyAreaWeekId,
            cancellationToken);
        if (studyAreaWeek is null)
        {
            throw new KeyNotFoundException($"StudyAreaWeek '{request.StudyAreaWeekId}' was not found.");
        }

        var currentWeek = _currentWeekProvider.GetCurrentWeek();
        var currentWeekStart = DateOnly.FromDateTime(currentWeek.Start);
        if (currentWeekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException("The current week must start on a Monday.");
        }

        if (studyAreaWeek.WeekStartDate != currentWeekStart)
        {
            throw new RecordNotInCurrentWeekException(
                "StudyRecord deletion is only allowed for the current week.");
        }

        var isoWeek = _isoWeekCalendar.GetIsoWeek(studyAreaWeek.WeekStartDate);
        var weeklyAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
            isoWeek.Year,
            isoWeek.WeekNumber,
            cancellationToken);
        if (weeklyAssessment is null)
        {
            throw new DomainException("The WeeklyAssessment for the configured week was not found.");
        }

        if (weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException(
                "The WeeklyAssessment associated with the StudyAreaWeek does not match its ISO week.");
        }

        var studyAreaWeekAssessment = await _studyAreaWeekAssessmentRepository.GetByStudyAreaWeekIdAsync(
            studyAreaWeek.Id,
            cancellationToken);
        if (studyAreaWeekAssessment is null || studyAreaWeekAssessment.StudyAreaWeekId != studyAreaWeek.Id)
        {
            throw new DomainException(
                $"StudyAreaWeekAssessment is missing or mismatched for StudyAreaWeek '{studyAreaWeek.Id}'.");
        }

        var studyAreaWeeks = await _studyAreaWeekRepository.GetByWeekStartDateAsync(
            studyAreaWeek.WeekStartDate,
            cancellationToken);
        if (studyAreaWeeks.Count == 0)
        {
            throw new DomainException("No StudyAreaWeek configuration was found for the current week.");
        }

        foreach (var configuredWeek in studyAreaWeeks)
        {
            if (configuredWeek.WeeklyAssessmentId != weeklyAssessment.Id)
            {
                throw new DomainException(
                    "All StudyAreaWeeks in the same week must reference the same WeeklyAssessment.");
            }
        }

        var eligibleRecord = await _studyRecordLifoSelector.SelectLastEligibleAsync(
            studyAreaWeek.Id,
            cancellationToken);
        if (eligibleRecord is null)
        {
            throw new NoEligibleStudyRecordException("No eligible study record was found.");
        }

        if (eligibleRecord.StudyAreaWeekId != studyAreaWeek.Id)
        {
            throw new DomainException("The selected StudyRecord does not belong to the requested StudyAreaWeek.");
        }

        _studyRecordRepository.Delete(eligibleRecord);
        await _weeklyAssessmentSynchronizer.SynchronizeAsync(
            weeklyAssessment.Id,
            cancellationToken);

        return Unit.Value;
    }
}