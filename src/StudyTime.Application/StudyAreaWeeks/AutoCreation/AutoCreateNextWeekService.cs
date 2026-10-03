using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreaWeeks.AutoCreation;

public sealed class AutoCreateNextWeekService(
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IAutoCreateNextWeekService
{
    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository
        ?? throw new ArgumentNullException(nameof(studyAreaRepository));

    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository
        ?? throw new ArgumentNullException(nameof(studyPlanRepository));

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

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider
        ?? throw new ArgumentNullException(nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar
        ?? throw new ArgumentNullException(nameof(isoWeekCalendar));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var currentWeek = _currentWeekProvider.GetCurrentWeek();
            var currentWeekStart =
                DateOnly.FromDateTime(currentWeek.Start);

            if (currentWeekStart.DayOfWeek != DayOfWeek.Monday)
            {
                throw new DomainException(
                    "The current week must start on a Monday.");
            }

            var nextWeekStart = currentWeekStart.AddDays(7);

            var nextWeekStudyAreaWeeks =
                await _studyAreaWeekRepository.GetByWeekStartDateAsync(
                    nextWeekStart,
                    cancellationToken);

            if (nextWeekStudyAreaWeeks.Count > 0)
            {
                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);

                return;
            }

            var currentWeekStudyAreaWeeks =
                await _studyAreaWeekRepository.GetByWeekStartDateAsync(
                    currentWeekStart,
                    cancellationToken);

            if (currentWeekStudyAreaWeeks.Count == 0)
            {
                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);

                return;
            }

            var nextIsoWeek =
                _isoWeekCalendar.GetIsoWeek(nextWeekStart);

            var preparedItems =
                new List<PreparedItem>(
                    currentWeekStudyAreaWeeks.Count);

            foreach (var currentStudyAreaWeek in currentWeekStudyAreaWeeks)
            {
                var studyArea =
                    await _studyAreaRepository.GetByIdAsync(
                        currentStudyAreaWeek.StudyAreaId,
                        cancellationToken);

                if (studyArea is null)
                {
                    throw new KeyNotFoundException(
                        $"StudyArea '{currentStudyAreaWeek.StudyAreaId}' " +
                        "was not found.");
                }

                var studyPlan =
                    await _studyPlanRepository.GetByIdAsync(
                        currentStudyAreaWeek.StudyPlanId,
                        cancellationToken);

                if (studyPlan is null)
                {
                    throw new KeyNotFoundException(
                        $"StudyPlan '{currentStudyAreaWeek.StudyPlanId}' " +
                        "was not found.");
                }

                var individualGoal =
                    GoalCalculator.CalculateIndividualGoal(
                        studyArea.StdWeekStudyTime.Value,
                        studyPlan.Coefficient.Value);

                preparedItems.Add(
                    new PreparedItem(
                        currentStudyAreaWeek,
                        individualGoal));
            }

            var globalGoal =
                GoalCalculator.CalculateGlobalGoal(
                    preparedItems.Select(item => item.IndividualGoal));

            WeeklyGoalValidator.Validate(globalGoal);

            var weeklyAssessment =
                await _weeklyAssessmentRepository
                    .GetByYearAndWeekNumberAsync(
                        nextIsoWeek.Year,
                        nextIsoWeek.WeekNumber,
                        cancellationToken);

            if (weeklyAssessment is null)
            {
                weeklyAssessment = new WeeklyAssessment(
                    nextIsoWeek.WeekNumber,
                    nextIsoWeek.Year,
                    globalGoal);

                await _weeklyAssessmentRepository.AddAsync(
                    weeklyAssessment,
                    cancellationToken);
            }
            else
            {
                if (weeklyAssessment.WeekNumber !=
                        nextIsoWeek.WeekNumber ||
                    weeklyAssessment.Year != nextIsoWeek.Year)
                {
                    throw new DomainException(
                        "The WeeklyAssessment does not match the " +
                        "ISO week being created.");
                }

                weeklyAssessment.UpdateGlobalGoal(globalGoal);
                weeklyAssessment.UpdateMinutesStudied(0);

                _weeklyAssessmentRepository.Update(
                    weeklyAssessment);
            }

            foreach (var item in preparedItems)
            {
                var newStudyAreaWeek = new StudyAreaWeek(
                    nextWeekStart,
                    item.Source.StudyAreaId,
                    item.Source.StudyPlanId,
                    weeklyAssessment.Id);

                var newAssessment =
                    new StudyAreaWeekAssessment(
                        item.IndividualGoal,
                        newStudyAreaWeek.Id);

                await _studyAreaWeekRepository.AddAsync(
                    newStudyAreaWeek,
                    cancellationToken);

                await _studyAreaWeekAssessmentRepository.AddAsync(
                    newAssessment,
                    cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
    }

    private sealed record PreparedItem(
        StudyAreaWeek Source,
        decimal IndividualGoal);
}