using MediatR;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyRecords.Commands;

public sealed class CreateStudyRecordHandler(
    IClock clock,
    IUnitOfWork unitOfWork,
    IIsoWeekCalendar isoWeekCalendar,
    ICurrentWeekProvider currentWeekProvider,
    IStudyRecordRepository studyRecordRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository)
    : IRequestHandler<CreateStudyRecordCommand, StudyRecordDto>
{
    private const string SaoPauloTimeZoneId = "America/Sao_Paulo";

    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository
        _studyAreaWeekAssessmentRepository = studyAreaWeekAssessmentRepository
            ?? throw new ArgumentNullException(nameof(studyAreaWeekAssessmentRepository));

    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<StudyRecordDto> Handle(CreateStudyRecordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyAreaWeek = await _studyAreaWeekRepository.GetByIdAsync(request.StudyAreaWeekId, cancellationToken);

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
            throw new RecordNotInCurrentWeekException("StudyRecord creation is only allowed for the current week.");
        }

        var isoWeek = _isoWeekCalendar.GetIsoWeek(studyAreaWeek.WeekStartDate);

        var weeklyAssessment =
            await _weeklyAssessmentRepository
                .GetByYearAndWeekNumberAsync(
                    isoWeek.Year,
                    isoWeek.WeekNumber,
                    cancellationToken);

        if (weeklyAssessment is null)
        {
            throw new DomainException("The WeeklyAssessment for the configured week was not found.");
        }

        if (weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException("The WeeklyAssessment associated with the StudyAreaWeek does not match its ISO week.");
        }

        var studyAreaWeeks = await _studyAreaWeekRepository
                .GetByWeekStartDateAsync(studyAreaWeek.WeekStartDate, cancellationToken);

        if (studyAreaWeeks.Count == 0)
        {
            throw new DomainException("No StudyAreaWeek configuration was found for the current week.");
        }

        var configuredAssessments = new List<StudyAreaWeekAssessment>(studyAreaWeeks.Count);
        var individualGoals = new List<decimal>(studyAreaWeeks.Count);
        foreach (var configuredWeek in studyAreaWeeks)
        {
            if (configuredWeek.WeeklyAssessmentId != weeklyAssessment.Id)
            {
                throw new DomainException(
                    "All StudyAreaWeeks in the same week must reference the same WeeklyAssessment.");
            }

            var configuredAssessment = await _studyAreaWeekAssessmentRepository
                .GetByStudyAreaWeekIdAsync(configuredWeek.Id, cancellationToken);
            if (configuredAssessment is null || configuredAssessment.StudyAreaWeekId != configuredWeek.Id)
            {
                throw new DomainException(
                    $"StudyAreaWeekAssessment is missing or mismatched for StudyAreaWeek '{configuredWeek.Id}'.");
            }

            configuredAssessments.Add(configuredAssessment);
            individualGoals.Add(configuredAssessment.WeekIndividualGoal);
        }

        var calculatedGlobalGoal = GoalCalculator.CalculateGlobalGoal(individualGoals);
        WeeklyGoalValidator.Validate(calculatedGlobalGoal);
        if (calculatedGlobalGoal != weeklyAssessment.WeekGlobalGoal)
        {
            throw new DomainException(
                "The WeeklyAssessment global goal is inconsistent with the individual goals of its configured week.");
        }

        var individualAssessment = configuredAssessments.Single(
            assessment => assessment.StudyAreaWeekId == studyAreaWeek.Id);

        var recordDate = GetCurrentLocalDate();

        WeekDateValidator.ValidateRecord(
            recordDate,
            studyAreaWeek.WeekStartDate,
            weeklyAssessment.WeekGlobalGoal);

        var studyRecord = new StudyRecord(
            recordDate,
            new Minutes(request.Minutes),
            studyAreaWeek.Id);

        await _studyRecordRepository.AddAsync(studyRecord, cancellationToken);

        var individualRecords =
            await _studyRecordRepository
                .GetByStudyAreaWeekIdAsync(studyAreaWeek.Id, cancellationToken);

        // The new StudyRecord is tracked by EF Core but has not been saved yet.
        // Repository queries use AsNoTracking and therefore return only persisted records.
        var individualMinutesStudied = SumMinutes(individualRecords.Append(studyRecord));
        individualAssessment.UpdateMinutesStudied(individualMinutesStudied);
        _studyAreaWeekAssessmentRepository.Update(individualAssessment);

        var totalMinutesStudied = 0;

        foreach (var configuredWeek in studyAreaWeeks)
        {
            var records = await _studyRecordRepository
                    .GetByStudyAreaWeekIdAsync(configuredWeek.Id, cancellationToken);

            var configuredWeekMinutes = SumMinutes(records);

            if (configuredWeek.Id == studyAreaWeek.Id)
            {
                configuredWeekMinutes = checked(configuredWeekMinutes + studyRecord.Minutes.Value);
            }

            totalMinutesStudied = checked(totalMinutesStudied + configuredWeekMinutes);
        }

        weeklyAssessment.UpdateMinutesStudied(totalMinutesStudied);
        _weeklyAssessmentRepository.Update(weeklyAssessment);

        if (individualAssessment.MinutesStudied !=
            individualRecords.Append(studyRecord).Sum(record => record.Minutes.Value))
        {
            throw new DomainException("The StudyAreaWeekAssessment minutes are inconsistent  with its StudyRecords.");
        }

        var confirmedGlobalMinutes = 0;

        foreach (var configuredWeek in studyAreaWeeks)
        {
            var records =
                await _studyRecordRepository
                .GetByStudyAreaWeekIdAsync(configuredWeek.Id, cancellationToken);

            var configuredWeekMinutes = SumMinutes(records);

            if (configuredWeek.Id == studyAreaWeek.Id)
            {
                configuredWeekMinutes = checked(configuredWeekMinutes + studyRecord.Minutes.Value);
            }

            confirmedGlobalMinutes = checked(confirmedGlobalMinutes + configuredWeekMinutes);
        }

        if (weeklyAssessment.MinutesStudied != confirmedGlobalMinutes)
        {
            throw new DomainException("The WeeklyAssessment minutes are inconsistent with the StudyRecords of its week.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudyRecordDto(
            studyRecord.Id,
            studyRecord.Date,
            studyRecord.CreatedAt,
            studyRecord.Minutes.Value,
            studyRecord.StudyAreaWeekId);
    }

    private DateOnly GetCurrentLocalDate()
    {
        var utcNow = DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);

        return DateOnly.FromDateTime(localNow);
    }

    private static int SumMinutes(IEnumerable<StudyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var total = 0;

        foreach (var record in records)
        {
            total = checked(total + record.Minutes.Value);
        }

        return total;
    }
}