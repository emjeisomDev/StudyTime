using MediatR;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyRecords.Commands;

public sealed class DeleteLastStudyRecordHandler(
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyRecordRepository studyRecordRepository,
    IStudyRecordLifoSelector studyRecordLifoSelector,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLastStudyRecordCommand, Unit>
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

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository
        ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    private readonly IStudyRecordLifoSelector _studyRecordLifoSelector =
        studyRecordLifoSelector
        ?? throw new ArgumentNullException(nameof(studyRecordLifoSelector));

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider
        ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Unit> Handle(
        DeleteLastStudyRecordCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.StudyAreaWeekId == Guid.Empty)
        {
            throw new ArgumentException(
                "StudyAreaWeekId is required.",
                nameof(request.StudyAreaWeekId));
        }

        var studyAreaWeek =
            await _studyAreaWeekRepository.GetByIdAsync(
                request.StudyAreaWeekId,
                cancellationToken);

        if (studyAreaWeek is null)
        {
            throw new KeyNotFoundException(
                $"StudyAreaWeek '{request.StudyAreaWeekId}' was not found.");
        }

        var currentWeek =
            _currentWeekProvider.GetCurrentWeek();

        var currentWeekStart =
            DateOnly.FromDateTime(currentWeek.Start);

        if (currentWeekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException(
                "The current week must start on a Monday.");
        }

        if (studyAreaWeek.WeekStartDate != currentWeekStart)
        {
            throw new RecordNotInCurrentWeekException(
                "StudyRecord deletion is only allowed for the current week.");
        }

        var isoWeek =
            _isoWeekCalendar.GetIsoWeek(
                studyAreaWeek.WeekStartDate);

        var weeklyAssessment =
            await _weeklyAssessmentRepository
                .GetByYearAndWeekNumberAsync(
                    isoWeek.Year,
                    isoWeek.WeekNumber,
                    cancellationToken);

        if (weeklyAssessment is null)
        {
            throw new DomainException(
                "The WeeklyAssessment for the configured week was not found.");
        }

        if (weeklyAssessment.Id !=
            studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException(
                "The WeeklyAssessment associated with the StudyAreaWeek " +
                "does not match its ISO week.");
        }

        var studyAreaWeekAssessment =
            await _studyAreaWeekAssessmentRepository
                .GetByStudyAreaWeekIdAsync(
                    studyAreaWeek.Id,
                    cancellationToken);

        if (studyAreaWeekAssessment is null)
        {
            throw new DomainException(
                "StudyAreaWeekAssessment was not found for " +
                $"StudyAreaWeek '{studyAreaWeek.Id}'.");
        }

        if (studyAreaWeekAssessment.StudyAreaWeekId !=
            studyAreaWeek.Id)
        {
            throw new DomainException(
                "The StudyAreaWeekAssessment does not belong to " +
                "the requested StudyAreaWeek.");
        }

        var studyAreaWeeks =
            await _studyAreaWeekRepository
                .GetByWeekStartDateAsync(
                    studyAreaWeek.WeekStartDate,
                    cancellationToken);

        if (studyAreaWeeks.Count == 0)
        {
            throw new DomainException(
                "No StudyAreaWeek configuration was found for the current week.");
        }

        foreach (var configuredWeek in studyAreaWeeks)
        {
            if (configuredWeek.WeeklyAssessmentId !=
                weeklyAssessment.Id)
            {
                throw new DomainException(
                    "All StudyAreaWeeks in the same week must reference " +
                    "the same WeeklyAssessment.");
            }
        }

        var eligibleRecord =
            await _studyRecordLifoSelector.SelectLastEligibleAsync(
                studyAreaWeek.Id,
                cancellationToken);

        if (eligibleRecord is null)
        {
            throw new NoEligibleStudyRecordException(
                "No eligible study record was found.");
        }

        if (eligibleRecord.StudyAreaWeekId !=
            studyAreaWeek.Id)
        {
            throw new DomainException(
                "The selected StudyRecord does not belong to " +
                "the requested StudyAreaWeek.");
        }

        _studyRecordRepository.Delete(
            eligibleRecord);

        var remainingRecords =
            await _studyRecordRepository
                .GetByStudyAreaWeekIdAsync(
                    studyAreaWeek.Id,
                    cancellationToken);

        var individualMinutesStudied =
            SumMinutes(remainingRecords);

        studyAreaWeekAssessment.UpdateMinutesStudied(
            individualMinutesStudied);

        _studyAreaWeekAssessmentRepository.Update(
            studyAreaWeekAssessment);

        var globalMinutesStudied = 0;

        foreach (var configuredWeek in studyAreaWeeks)
        {
            var assessment =
                await _studyAreaWeekAssessmentRepository
                    .GetByStudyAreaWeekIdAsync(
                        configuredWeek.Id,
                        cancellationToken);

            if (assessment is null)
            {
                throw new DomainException(
                    "StudyAreaWeekAssessment was not found for " +
                    $"StudyAreaWeek '{configuredWeek.Id}'.");
            }

            if (assessment.StudyAreaWeekId !=
                configuredWeek.Id)
            {
                throw new DomainException(
                    "A StudyAreaWeekAssessment does not belong to " +
                    "its StudyAreaWeek.");
            }

            var records =
                await _studyRecordRepository
                    .GetByStudyAreaWeekIdAsync(
                        configuredWeek.Id,
                        cancellationToken);

            var minutesStudied =
                SumMinutes(records);

            assessment.UpdateMinutesStudied(
                minutesStudied);

            _studyAreaWeekAssessmentRepository.Update(
                assessment);

            globalMinutesStudied =
                checked(
                    globalMinutesStudied +
                    minutesStudied);
        }

        weeklyAssessment.UpdateMinutesStudied(
            globalMinutesStudied);

        _weeklyAssessmentRepository.Update(
            weeklyAssessment);

        var confirmedIndividualMinutes =
            await _studyRecordRepository
                .GetByStudyAreaWeekIdAsync(
                    studyAreaWeek.Id,
                    cancellationToken);

        var confirmedIndividualTotal =
            SumMinutes(confirmedIndividualMinutes);

        if (studyAreaWeekAssessment.MinutesStudied !=
            confirmedIndividualTotal)
        {
            throw new DomainException(
                "The StudyAreaWeekAssessment minutes are inconsistent " +
                "with its StudyRecords.");
        }

        var confirmedGlobalMinutes = 0;

        foreach (var configuredWeek in studyAreaWeeks)
        {
            var records =
                await _studyRecordRepository
                    .GetByStudyAreaWeekIdAsync(
                        configuredWeek.Id,
                        cancellationToken);

            confirmedGlobalMinutes =
                checked(
                    confirmedGlobalMinutes +
                    SumMinutes(records));
        }

        if (weeklyAssessment.MinutesStudied !=
            confirmedGlobalMinutes)
        {
            throw new DomainException(
                "The WeeklyAssessment minutes are inconsistent " +
                "with the StudyRecords of its week.");
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Unit.Value;
    }

    private static int SumMinutes(
        IEnumerable<Domain.Entities.StudyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var total = 0;

        foreach (var record in records)
        {
            total = checked(
                total + record.Minutes.Value);
        }

        return total;
    }
}