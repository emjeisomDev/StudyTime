using MediatR;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed class UpdateStudyAreaWeekHandler(
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IStudyRecordRepository studyRecordRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStudyAreaWeekCommand, StudyAreaWeekDto>
{
    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository ?? throw new ArgumentNullException(nameof(studyAreaRepository));

    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository ?? throw new ArgumentNullException(nameof(studyPlanRepository));

    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository _studyAreaWeekAssessmentRepository =
        studyAreaWeekAssessmentRepository
        ?? throw new ArgumentNullException(nameof(studyAreaWeekAssessmentRepository));

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<StudyAreaWeekDto> Handle(UpdateStudyAreaWeekCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIdentifiers(request);

        var studyAreaWeek = await _studyAreaWeekRepository.GetByIdAsync(request.Id, cancellationToken);

        if (studyAreaWeek is null)
        {
            throw new KeyNotFoundException($"StudyAreaWeek '{request.Id}' was not found.");
        }

        await ValidateWeekLockAsync(cancellationToken);

        var studyArea = await _studyAreaRepository.GetByIdAsync(request.StudyAreaId, cancellationToken);

        if (studyArea is null)
        {
            throw new KeyNotFoundException($"StudyArea '{request.StudyAreaId}' was not found.");
        }

        var studyPlan = await _studyPlanRepository.GetByIdAsync(request.StudyPlanId, cancellationToken);

        if (studyPlan is null)
        {
            throw new KeyNotFoundException($"StudyPlan '{request.StudyPlanId}' was not found.");
        }

        if (studyPlan.Status != StudyPlanStatus.Active)
        {
            throw new DomainException($"StudyPlan '{request.StudyPlanId}' is inactive.");
        }

        if (await _studyAreaWeekRepository.ExistsForStudyAreaInWeekAsync(
                request.StudyAreaId,
                studyAreaWeek.WeekStartDate,
                cancellationToken)
            && studyAreaWeek.StudyAreaId != request.StudyAreaId)
        {
            throw new DuplicateStudyAreaWeekException(
                $"StudyArea '{request.StudyAreaId}' is already configured " +
                $"for week '{studyAreaWeek.WeekStartDate:yyyy-MM-dd}'.");
        }

        var assessment = await _studyAreaWeekAssessmentRepository
            .GetByStudyAreaWeekIdAsync(studyAreaWeek.Id, cancellationToken);

        if (assessment is null)
        {
            throw new DomainException(
                $"StudyAreaWeekAssessment was not found for " +
                $"StudyAreaWeek '{studyAreaWeek.Id}'.");
        }

        var records = await _studyRecordRepository
            .GetByStudyAreaWeekIdAsync(studyAreaWeek.Id, cancellationToken);

        var individualGoal = GoalCalculator
            .CalculateIndividualGoal(studyArea.StdWeekStudyTime.Value, studyPlan.Coefficient.Value);

        var allStudyAreaWeeks =
            await _studyAreaWeekRepository
                .GetByWeekStartDateAsync(studyAreaWeek.WeekStartDate, cancellationToken);

        var individualGoals = new List<decimal>(allStudyAreaWeeks.Count);

        foreach (var currentWeek in allStudyAreaWeeks)
        {
            if (currentWeek.Id == studyAreaWeek.Id)
            {
                individualGoals.Add(individualGoal);
                continue;
            }

            var currentAssessment =
                await _studyAreaWeekAssessmentRepository.GetByStudyAreaWeekIdAsync(
                    currentWeek.Id,
                    cancellationToken);

            if (currentAssessment is null)
            {
                throw new DomainException(
                    $"StudyAreaWeekAssessment was not found for " +
                    $"StudyAreaWeek '{currentWeek.Id}'.");
            }

            individualGoals.Add(currentAssessment.WeekIndividualGoal);
        }

        var globalGoal = GoalCalculator.CalculateGlobalGoal(individualGoals);
        WeeklyGoalValidator.Validate(globalGoal);

        var isoWeek = _isoWeekCalendar.GetIsoWeek(studyAreaWeek.WeekStartDate);

        var weeklyAssessment =
            await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (weeklyAssessment is null ||
            weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException(
                "The weekly assessment associated with the configuration " +
                "does not match its ISO week.");
        }

        var minutesStudied = records.Sum(record => record.Minutes.Value);

        var totalMinutesStudied = 0;

        foreach (var currentWeek in allStudyAreaWeeks)
        {
            var currentRecords =
                await _studyRecordRepository.GetByStudyAreaWeekIdAsync(
                    currentWeek.Id,
                    cancellationToken);

            totalMinutesStudied = checked(
                totalMinutesStudied +
                currentRecords.Sum(record => record.Minutes.Value));
        }

        var updatedStudyAreaWeek = new StudyAreaWeek(
            studyAreaWeek.Id,
            studyAreaWeek.WeekStartDate,
            studyArea.Id,
            studyPlan.Id,
            studyAreaWeek.WeeklyAssessmentId);

        assessment.UpdateIndividualGoal(individualGoal);
        assessment.UpdateMinutesStudied(minutesStudied);
        weeklyAssessment.UpdateGlobalGoal(globalGoal);
        weeklyAssessment.UpdateMinutesStudied(totalMinutesStudied);

        _studyAreaWeekRepository.Update(updatedStudyAreaWeek);
        _studyAreaWeekAssessmentRepository.Update(assessment);
        _weeklyAssessmentRepository.Update(weeklyAssessment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudyAreaWeekDto(
            updatedStudyAreaWeek.Id,
            updatedStudyAreaWeek.WeekStartDate,
            updatedStudyAreaWeek.StudyAreaId,
            updatedStudyAreaWeek.StudyPlanId,
            updatedStudyAreaWeek.WeeklyAssessmentId,
            assessment.WeekIndividualGoal,
            assessment.MinutesStudied,
            GoalAchievedEvaluator.EvaluateIndividual(assessment));
    }

    private static void ValidateIdentifiers(UpdateStudyAreaWeekCommand request)
    {
        if (request.Id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(request.Id));
        }

        if (request.StudyAreaId == Guid.Empty)
        {
            throw new ArgumentException("StudyAreaId is required.", nameof(request.StudyAreaId));
        }

        if (request.StudyPlanId == Guid.Empty)
        {
            throw new ArgumentException("StudyPlanId is required.", nameof(request.StudyPlanId));
        }
    }

    private async Task ValidateWeekLockAsync(CancellationToken cancellationToken)
    {
        var currentWeek = _currentWeekProvider.GetCurrentWeek();
        var currentWeekStart = DateOnly.FromDateTime(currentWeek.Start);
        var isoWeek = _isoWeekCalendar.GetIsoWeek(currentWeekStart);

        var currentAssessment =
            await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (currentAssessment is null ||
            currentAssessment.MinutesStudied < currentAssessment.WeekGlobalGoal)
        {
            throw new WeekConfigurationLockedException(
                "Weekly configuration can only be changed after the " +
                "current week's global goal has been reached.");
        }
    }
}