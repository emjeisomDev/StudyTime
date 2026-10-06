using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Services;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Application.StudyRecords.Commands;

public sealed class CreateStudyRecordHandler(
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyRecordRepository studyRecordRepository,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IClock clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStudyRecordCommand, StudyRecordDto>
{
    private const string SaoPauloTimeZoneId = "America/Sao_Paulo";

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

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider
        ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<StudyRecordDto> Handle(
        CreateStudyRecordCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyAreaWeek =
            await _studyAreaWeekRepository.GetByIdAsync(
                request.StudyAreaWeekId,
                cancellationToken);

        if (studyAreaWeek is null)
        {
            throw new KeyNotFoundException(
                $"StudyAreaWeek '{request.StudyAreaWeekId}' was not found.");
        }

        var currentWeek = _currentWeekProvider.GetCurrentWeek();
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
                "StudyRecord creation is only allowed for the current week.");
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

        if (weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException(
                "The WeeklyAssessment associated with the StudyAreaWeek " +
                "does not match its ISO week.");
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
            if (configuredWeek.WeeklyAssessmentId != weeklyAssessment.Id)
            {
                throw new DomainException(
                    "All StudyAreaWeeks in the same week must reference " +
                    "the same WeeklyAssessment.");
            }
        }

        WeeklyGoalValidator.Validate(
            weeklyAssessment.WeekGlobalGoal);

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
                "the requested StudyAreaWeek.");
        }

        var recordDate = GetCurrentLocalDate();

        WeekDateValidator.ValidateRecord(
            recordDate,
            studyAreaWeek.WeekStartDate,
            weeklyAssessment.WeekGlobalGoal);

        var studyRecord = new StudyRecord(
            recordDate,
            new Minutes(request.Minutes),
            studyAreaWeek.Id);

        await _studyRecordRepository.AddAsync(
            studyRecord,
            cancellationToken);

        var individualRecords =
            await _studyRecordRepository
                .GetByStudyAreaWeekIdAsync(
                    studyAreaWeek.Id,
                    cancellationToken);

        var individualMinutesStudied =
            SumMinutes(individualRecords);

        individualAssessment.UpdateMinutesStudied(
            individualMinutesStudied);

        _studyAreaWeekAssessmentRepository.Update(
            individualAssessment);

        var totalMinutesStudied = 0;

        foreach (var configuredWeek in studyAreaWeeks)
        {
            var records =
                await _studyRecordRepository
                    .GetByStudyAreaWeekIdAsync(
                        configuredWeek.Id,
                        cancellationToken);

            var configuredWeekMinutes =
                SumMinutes(records);

            totalMinutesStudied =
                checked(totalMinutesStudied + configuredWeekMinutes);
        }

        weeklyAssessment.UpdateMinutesStudied(
            totalMinutesStudied);

        _weeklyAssessmentRepository.Update(
            weeklyAssessment);

        if (individualAssessment.MinutesStudied !=
            individualRecords.Sum(record => record.Minutes.Value))
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

        return new StudyRecordDto(
            studyRecord.Id,
            studyRecord.Date,
            studyRecord.CreatedAt,
            studyRecord.Minutes.Value,
            studyRecord.StudyAreaWeekId);
    }

    private DateOnly GetCurrentLocalDate()
    {
        var utcNow = DateTime.SpecifyKind(
            _clock.UtcNow,
            DateTimeKind.Utc);

        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                SaoPauloTimeZoneId);

        var localNow =
            TimeZoneInfo.ConvertTimeFromUtc(
                utcNow,
                timeZone);

        return DateOnly.FromDateTime(localNow);
    }

    private static int SumMinutes(
        IEnumerable<StudyRecord> records)
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