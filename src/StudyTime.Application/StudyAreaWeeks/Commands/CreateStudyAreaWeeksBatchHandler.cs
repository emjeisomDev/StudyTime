using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Services;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed class CreateStudyAreaWeeksBatchHandler(
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IRequestHandler<
        CreateStudyAreaWeeksBatchCommand,
        IReadOnlyList<StudyAreaWeekDto>>
{
    private readonly IStudyAreaRepository _studyAreaRepository =
        studyAreaRepository ?? throw new ArgumentNullException(
            nameof(studyAreaRepository));

    private readonly IStudyPlanRepository _studyPlanRepository =
        studyPlanRepository ?? throw new ArgumentNullException(
            nameof(studyPlanRepository));

    private readonly IStudyAreaWeekRepository _studyAreaWeekRepository =
        studyAreaWeekRepository ?? throw new ArgumentNullException(
            nameof(studyAreaWeekRepository));

    private readonly IStudyAreaWeekAssessmentRepository
        _studyAreaWeekAssessmentRepository =
            studyAreaWeekAssessmentRepository
            ?? throw new ArgumentNullException(
                nameof(studyAreaWeekAssessmentRepository));

    private readonly IWeeklyAssessmentRepository
        _weeklyAssessmentRepository =
            weeklyAssessmentRepository
            ?? throw new ArgumentNullException(
                nameof(weeklyAssessmentRepository));

    private readonly ICurrentWeekProvider _currentWeekProvider =
        currentWeekProvider ?? throw new ArgumentNullException(
            nameof(currentWeekProvider));

    private readonly IIsoWeekCalendar _isoWeekCalendar =
        isoWeekCalendar ?? throw new ArgumentNullException(
            nameof(isoWeekCalendar));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(
            nameof(unitOfWork));

    public async Task<IReadOnlyList<StudyAreaWeekDto>> Handle(
        CreateStudyAreaWeeksBatchCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var currentWeek = _currentWeekProvider.GetCurrentWeek();
        var currentWeekStart = DateOnly.FromDateTime(currentWeek.Start);
        var lastAllowedWeekStart = currentWeekStart.AddDays(28);

        if (request.WeekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException(
                "The configured week must start on a Monday.");
        }

        if (request.WeekStartDate < currentWeekStart ||
            request.WeekStartDate > lastAllowedWeekStart)
        {
            throw new DomainException(
                "The configured week must be the current week or one of " +
                "the four following weeks.");
        }

        await ValidateWeekLockAsync(cancellationToken);

        var existingStudyAreaWeeks =
            await _studyAreaWeekRepository.GetByWeekStartDateAsync(
                request.WeekStartDate,
                cancellationToken);

        var requestedAreaIds = new HashSet<Guid>();

        foreach (var item in request.Items)
        {
            if (!requestedAreaIds.Add(item.StudyAreaId))
            {
                throw new DuplicateStudyAreaWeekException(
                    $"StudyArea '{item.StudyAreaId}' occurs more than once " +
                    "in the batch.");
            }

            if (await _studyAreaWeekRepository.ExistsForStudyAreaInWeekAsync(
                    item.StudyAreaId,
                    request.WeekStartDate,
                    cancellationToken))
            {
                throw new DuplicateStudyAreaWeekException(
                    $"StudyArea '{item.StudyAreaId}' is already configured " +
                    $"for week '{request.WeekStartDate:yyyy-MM-dd}'.");
            }
        }

        var preparedItems = new List<PreparedItem>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var studyArea = await _studyAreaRepository.GetByIdAsync
                                (item.StudyAreaId, cancellationToken);

            if (studyArea is null)
            {
                throw new KeyNotFoundException($"StudyArea '{item.StudyAreaId}' was not found.");
            }

            var studyPlan = await _studyPlanRepository.GetByIdAsync(item.StudyPlanId, cancellationToken);

            if (studyPlan is null)
            {
                throw new KeyNotFoundException($"StudyPlan '{item.StudyPlanId}' was not found.");
            }

            if (studyPlan.Status != StudyPlanStatus.Active)
            {
                throw new DomainException(
                    @"An inactive StudyPlan cannot be 
                    used in a new weekly configuration.");
            }

            var individualGoal = GoalCalculator.CalculateIndividualGoal(
                    studyArea.StdWeekStudyTime.Value,
                    studyPlan.Coefficient.Value);

            preparedItems.Add(new PreparedItem(
                    studyArea,
                    studyPlan,
                    individualGoal));
        }

        var individualGoals = new List<decimal>(existingStudyAreaWeeks.Count + preparedItems.Count);

        foreach (var existingStudyAreaWeek in existingStudyAreaWeeks)
        {
            var assessment = await _studyAreaWeekAssessmentRepository
                    .GetByStudyAreaWeekIdAsync(
                        existingStudyAreaWeek.Id,
                        cancellationToken);

            if (assessment is null)
            {
                throw new DomainException(
                    "StudyAreaWeekAssessment was not found for " +
                    $"StudyAreaWeek '{existingStudyAreaWeek.Id}'.");
            }

            individualGoals.Add(assessment.WeekIndividualGoal);
        }

        individualGoals.AddRange(preparedItems.Select(item => item.IndividualGoal));

        var globalGoal = GoalCalculator.CalculateGlobalGoal(individualGoals);

        WeeklyGoalValidator.Validate(globalGoal);

        var isoWeek = _isoWeekCalendar.GetIsoWeek(request.WeekStartDate);

        var weeklyAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (weeklyAssessment is null)
        {
            weeklyAssessment = new WeeklyAssessment(
                isoWeek.WeekNumber,
                isoWeek.Year,
                globalGoal);

            await _weeklyAssessmentRepository.AddAsync(weeklyAssessment, cancellationToken);
        }
        else
        {
            weeklyAssessment.UpdateGlobalGoal(globalGoal);
            _weeklyAssessmentRepository.Update(weeklyAssessment);
        }

        var result = new List<StudyAreaWeekDto>(
            preparedItems.Count);

        foreach (var item in preparedItems)
        {
            var studyAreaWeek = new StudyAreaWeek(
                request.WeekStartDate,
                item.StudyArea.Id,
                item.StudyPlan.Id,
                weeklyAssessment.Id);

            var assessment = new StudyAreaWeekAssessment(item.IndividualGoal, studyAreaWeek.Id);

            await _studyAreaWeekRepository.AddAsync(studyAreaWeek, cancellationToken);
            await _studyAreaWeekAssessmentRepository.AddAsync(assessment, cancellationToken);

            result.Add(
                new StudyAreaWeekDto(
                    studyAreaWeek.Id,
                    studyAreaWeek.WeekStartDate,
                    studyAreaWeek.StudyAreaId,
                    studyAreaWeek.StudyPlanId,
                    studyAreaWeek.WeeklyAssessmentId,
                    assessment.WeekIndividualGoal,
                    assessment.MinutesStudied,
                    GoalAchievedEvaluator.EvaluateIndividual(assessment)));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    private static void ValidateRequest(
        CreateStudyAreaWeeksBatchCommand request)
    {
        if (request.Items is null)
        {
            throw new ArgumentException("Items are required.", nameof(request.Items));
        }

        if (request.Items.Count == 0)
        {
            throw new ArgumentException("At least one item is required.", nameof(request.Items));
        }

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                throw new ArgumentException("Items cannot contain null entries.", nameof(request.Items));
            }

            if (item.StudyAreaId == Guid.Empty)
            {
                throw new ArgumentException("StudyAreaId is required.", nameof(request.Items));
            }

            if (item.StudyPlanId == Guid.Empty)
            {
                throw new ArgumentException("StudyPlanId is required.", nameof(request.Items));
            }
        }
    }

    private async Task ValidateWeekLockAsync(CancellationToken cancellationToken)
    {
        var currentWeek = _currentWeekProvider.GetCurrentWeek();
        var currentWeekStart = DateOnly.FromDateTime(currentWeek.Start);
        var isoWeek = _isoWeekCalendar.GetIsoWeek(currentWeekStart);

        var currentAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (currentAssessment is null || currentAssessment.MinutesStudied < currentAssessment.WeekGlobalGoal)
        {
            throw new WeekConfigurationLockedException(
                @"Weekly configuration can only be changed after 
                the current week's global goal has been reached.");
        }
    }

    private sealed record PreparedItem(
        StudyArea StudyArea,
        StudyPlan StudyPlan,
        decimal IndividualGoal);
}