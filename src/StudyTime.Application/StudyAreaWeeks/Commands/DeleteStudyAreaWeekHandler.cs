using MediatR;
using StudyTime.Domain.Services;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed class DeleteStudyAreaWeekHandler(
    IUnitOfWork unitOfWork,
    IIsoWeekCalendar isoWeekCalendar,
    ICurrentWeekProvider currentWeekProvider,
    IStudyRecordRepository studyRecordRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository
    )
    : IRequestHandler<DeleteStudyAreaWeekCommand, Unit>
{
    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository
        ?? throw new ArgumentNullException(nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository
        _studyAreaWeekAssessmentRepository =
            studyAreaWeekAssessmentRepository
            ?? throw new ArgumentNullException(
                nameof(studyAreaWeekAssessmentRepository));

    private readonly IStudyRecordRepository _studyRecordRepository =
        studyRecordRepository
        ?? throw new ArgumentNullException(nameof(studyRecordRepository));

    private readonly IWeeklyAssessmentRepository _weeklyAssessmentRepository =
        weeklyAssessmentRepository
        ?? throw new ArgumentNullException(nameof(weeklyAssessmentRepository));

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider
        ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Unit> Handle(
        DeleteStudyAreaWeekCommand request,
        CancellationToken cancellationToken)
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
            throw new KeyNotFoundException($"StudyAreaWeek '{request.Id}' was not found.");
        }

        var isoWeek = _isoWeekCalendar.GetIsoWeek(studyAreaWeek.WeekStartDate);

        var weeklyAssessment =
            await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (weeklyAssessment is null ||
            weeklyAssessment.Id != studyAreaWeek.WeeklyAssessmentId)
        {
            throw new DomainException(@"The weekly assessment associated with the 
											configuration does not match its ISO week.");
        }

        await ValidateWeekLockAsync(cancellationToken);

        var studyAreaWeeks =
            await _studyAreaWeekRepository.GetByWeekStartDateAsync(
                studyAreaWeek.WeekStartDate,
                cancellationToken);

        if (!studyAreaWeeks.Any(item => item.Id == studyAreaWeek.Id))
        {
            throw new DomainException("The StudyAreaWeek does not belong to its configured week.");
        }

        var assessment =
            await _studyAreaWeekAssessmentRepository
                .GetByStudyAreaWeekIdAsync(studyAreaWeek.Id, cancellationToken);

        if (assessment is null)
        {
            throw new DomainException($"StudyAreaWeekAssessment was not found for StudyAreaWeek '{studyAreaWeek.Id}'.");
        }

        var records = await _studyRecordRepository.GetByStudyAreaWeekIdAsync(studyAreaWeek.Id, cancellationToken);

        var remainingWeeks = studyAreaWeeks
            .Where(item => item.Id != studyAreaWeek.Id)
            .ToArray();

        foreach (var record in records)
        {
            _studyRecordRepository.Delete(record);
        }

        _studyAreaWeekAssessmentRepository.Delete(assessment);
        _studyAreaWeekRepository.Delete(studyAreaWeek);

        if (remainingWeeks.Length == 0)
        {
            _weeklyAssessmentRepository.Delete(weeklyAssessment);
        }
        else
        {
            var remainingGoals = new List<decimal>(remainingWeeks.Length);
            var totalMinutesStudied = 0;

            foreach (var remainingWeek in remainingWeeks)
            {
                if (remainingWeek.WeeklyAssessmentId != weeklyAssessment.Id)
                {
                    throw new DomainException("All StudyAreaWeeks in the same week must reference the same WeeklyAssessment.");
                }

                var remainingAssessment =
                    await _studyAreaWeekAssessmentRepository
                        .GetByStudyAreaWeekIdAsync(remainingWeek.Id, cancellationToken);

                if (remainingAssessment is null)
                {
                    throw new DomainException($"StudyAreaWeekAssessment was not found for StudyAreaWeek '{remainingWeek.Id}'.");
                }

                remainingGoals.Add(remainingAssessment.WeekIndividualGoal);

                var remainingRecords =
                    await _studyRecordRepository.GetByStudyAreaWeekIdAsync(remainingWeek.Id, cancellationToken);

                var weekMinutes = remainingRecords.Sum(
                    record => record.Minutes.Value);

                remainingAssessment.UpdateMinutesStudied(weekMinutes);

                _studyAreaWeekAssessmentRepository.Update(remainingAssessment);

                totalMinutesStudied = checked(totalMinutesStudied + weekMinutes);
            }

            var globalGoal = GoalCalculator.CalculateGlobalGoal(remainingGoals);
            WeeklyGoalValidator.Validate(globalGoal);
            weeklyAssessment.UpdateGlobalGoal(globalGoal);
            weeklyAssessment.UpdateMinutesStudied(totalMinutesStudied);
            _weeklyAssessmentRepository.Update(weeklyAssessment);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
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