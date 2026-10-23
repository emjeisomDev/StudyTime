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
    IUnitOfWork unitOfWork,
    IIsoWeekCalendar isoWeekCalendar,
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    ICurrentWeekProvider currentWeekProvider,
    IStudyRecordRepository studyRecordRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository)
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

        var currentWeekStart = DateOnly.FromDateTime(_currentWeekProvider.GetCurrentWeek().Start);
        var lastAllowedWeekStart = currentWeekStart.AddDays(28);
        if (studyAreaWeek.WeekStartDate.DayOfWeek != DayOfWeek.Monday ||
            studyAreaWeek.WeekStartDate < currentWeekStart ||
            studyAreaWeek.WeekStartDate > lastAllowedWeekStart)
        {
            throw new DomainException("Only the current week and the four following weeks can be changed manually.");
        }

        await ValidateWeekLockAsync(cancellationToken);

        var allStudyAreaWeeks = await _studyAreaWeekRepository.GetByWeekStartDateAsync(
            studyAreaWeek.WeekStartDate,
            cancellationToken);
        if (!allStudyAreaWeeks.Any(item => item.Id == studyAreaWeek.Id))
        {
            throw new DomainException("The StudyAreaWeek does not belong to its configured week.");
        }

        if (allStudyAreaWeeks.Any(item => item.WeeklyAssessmentId != studyAreaWeek.WeeklyAssessmentId))
        {
            throw new DomainException("All StudyAreaWeeks in the same week must reference the same WeeklyAssessment.");
        }

        var isoWeek = _isoWeekCalendar.GetIsoWeek(studyAreaWeek.WeekStartDate);
        var weeklyAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
            isoWeek.Year,
            isoWeek.WeekNumber,
            cancellationToken);
        if (weeklyAssessment is null || weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException("The weekly assessment associated with the configuration does not match its ISO week.");
        }

        var assessment = await _studyAreaWeekAssessmentRepository.GetByStudyAreaWeekIdAsync(
            studyAreaWeek.Id,
            cancellationToken);
        if (assessment is null || assessment.StudyAreaWeekId != studyAreaWeek.Id)
        {
            throw new DomainException($"StudyAreaWeekAssessment is missing or mismatched for StudyAreaWeek '{studyAreaWeek.Id}'.");
        }

        // A PUT with unchanged foreign keys is a no-op. It must not recalculate a historical
        // goal because StudyArea.std_week_study_time or StudyPlan.coefficient changed later.
        if (studyAreaWeek.StudyAreaId == request.StudyAreaId &&
            studyAreaWeek.StudyPlanId == request.StudyPlanId)
        {
            return new StudyAreaWeekDto(
                studyAreaWeek.Id,
                studyAreaWeek.WeekStartDate,
                studyAreaWeek.StudyAreaId,
                studyAreaWeek.StudyPlanId,
                studyAreaWeek.WeeklyAssessmentId,
                assessment.WeekIndividualGoal,
                assessment.MinutesStudied,
                GoalAchievedEvaluator.EvaluateIndividual(assessment));
        }

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
                $"StudyArea '{request.StudyAreaId}' is already configured for week '{studyAreaWeek.WeekStartDate:yyyy-MM-dd}'.");
        }

        var individualGoal = GoalCalculator.CalculateIndividualGoal(
            studyArea.StdWeekStudyTime.Value,
            studyPlan.Coefficient.Value);

        var individualGoals = new List<decimal>(allStudyAreaWeeks.Count);
        var totalMinutesStudied = 0;

        foreach (var configuredWeek in allStudyAreaWeeks)
        {
            var configuredAssessment = configuredWeek.Id == studyAreaWeek.Id
                ? assessment
                : await _studyAreaWeekAssessmentRepository.GetByStudyAreaWeekIdAsync(
                    configuredWeek.Id,
                    cancellationToken);

            if (configuredAssessment is null || configuredAssessment.StudyAreaWeekId != configuredWeek.Id)
            {
                throw new DomainException(
                    $"StudyAreaWeekAssessment is missing or mismatched for StudyAreaWeek '{configuredWeek.Id}'.");
            }

            var records = await _studyRecordRepository.GetByStudyAreaWeekIdAsync(
                configuredWeek.Id,
                cancellationToken);
            var actualMinutes = records.Sum(record => record.Minutes.Value);
            configuredAssessment.UpdateMinutesStudied(actualMinutes);
            _studyAreaWeekAssessmentRepository.Update(configuredAssessment);
            totalMinutesStudied = checked(totalMinutesStudied + actualMinutes);

            individualGoals.Add(configuredWeek.Id == studyAreaWeek.Id
                ? individualGoal
                : configuredAssessment.WeekIndividualGoal);
        }

        var globalGoal = GoalCalculator.CalculateGlobalGoal(individualGoals);
        WeeklyGoalValidator.Validate(globalGoal);

        var updatedStudyAreaWeek = new StudyAreaWeek(
            studyAreaWeek.Id,
            studyAreaWeek.WeekStartDate,
            studyArea.Id,
            studyPlan.Id,
            studyAreaWeek.WeeklyAssessmentId);

        assessment.UpdateIndividualGoal(individualGoal);
        // The target assessment was already recalculated from persisted records in the loop.
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
        var currentWeekStart = DateOnly.FromDateTime(_currentWeekProvider.GetCurrentWeek().Start);
        var isoWeek = _isoWeekCalendar.GetIsoWeek(currentWeekStart);
        var currentAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
            isoWeek.Year, isoWeek.WeekNumber, cancellationToken);

        if (currentAssessment is null)
        {
            throw new WeekConfigurationLockedException(
                "The current week is not configured or its goal has not been achieved.");
        }

        var configuredWeeks = await _studyAreaWeekRepository.GetByWeekStartDateAsync(
            currentWeekStart, cancellationToken);
        if (configuredWeeks.Count == 0)
        {
            throw new WeekConfigurationLockedException(
                "The current week is not configured or its goal has not been achieved.");
        }

        foreach (var configuredWeek in configuredWeeks)
        {
            if (configuredWeek.WeeklyAssessmentId != currentAssessment.Id)
            {
                throw new DomainException(
                    "All StudyAreaWeeks in the current week must reference its WeeklyAssessment.");
            }

            var individualAssessment = await _studyAreaWeekAssessmentRepository
                .GetByStudyAreaWeekIdAsync(configuredWeek.Id, cancellationToken);
            if (individualAssessment is null)
            {
                throw new DomainException(
                    $"StudyAreaWeekAssessment was not found for StudyAreaWeek '{configuredWeek.Id}'.");
            }

            if (!GoalAchievedEvaluator.EvaluateIndividual(individualAssessment))
            {
                throw new WeekConfigurationLockedException(
                    "Weekly configuration can only be changed after every individual goal in the current week has been reached.");
            }
        }
    }
}