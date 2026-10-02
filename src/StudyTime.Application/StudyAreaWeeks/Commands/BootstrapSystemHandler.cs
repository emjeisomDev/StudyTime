using MediatR;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Services;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed class BootstrapSystemHandler(
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BootstrapSystemCommand, IReadOnlyList<StudyAreaWeekDto>>
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
        BootstrapSystemCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var existingStudyAreaWeeks =
            await _studyAreaWeekRepository.GetAllAsync(cancellationToken);

        if (existingStudyAreaWeeks.Count > 0)
        {
            throw new DomainException(
                "The system has already been initialized.");
        }

        var weekStartDate = DateOnly.FromDateTime(
            _currentWeekProvider.GetCurrentWeek().Start);

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException(
                "The current week must start on a Monday.");
        }

        var requestedAreaIds = new HashSet<Guid>();

        foreach (var item in request.Items)
        {
            if (!requestedAreaIds.Add(item.StudyAreaId))
            {
                throw new DuplicateStudyAreaWeekException(
                    $"StudyArea '{item.StudyAreaId}' occurs more than once " +
                    "in the bootstrap configuration.");
            }
        }

        var preparedItems = new List<PreparedItem>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var studyArea = await _studyAreaRepository.GetByIdAsync(
                item.StudyAreaId,
                cancellationToken);

            if (studyArea is null)
            {
                throw new KeyNotFoundException(
                    $"StudyArea '{item.StudyAreaId}' was not found.");
            }

            var studyPlan = await _studyPlanRepository.GetByIdAsync(
                item.StudyPlanId,
                cancellationToken);

            if (studyPlan is null)
            {
                throw new KeyNotFoundException(
                    $"StudyPlan '{item.StudyPlanId}' was not found.");
            }

            if (studyPlan.Status != StudyPlanStatus.Active)
            {
                throw new DomainException(
                    $"StudyPlan '{item.StudyPlanId}' is inactive.");
            }

            var individualGoal = GoalCalculator.CalculateIndividualGoal(
                studyArea.StdWeekStudyTime.Value,
                studyPlan.Coefficient.Value);

            preparedItems.Add(
                new PreparedItem(studyArea, studyPlan, individualGoal));
        }

        var globalGoal = GoalCalculator.CalculateGlobalGoal(
            preparedItems.Select(item => item.IndividualGoal));

        WeeklyGoalValidator.Validate(globalGoal);

        var isoWeek = _isoWeekCalendar.GetIsoWeek(weekStartDate);

        var weeklyAssessment =
            await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (weeklyAssessment is not null)
        {
            throw new DomainException(
                "A weekly assessment already exists for the bootstrap week, " +
                "but no StudyAreaWeek was found.");
        }

        weeklyAssessment = new WeeklyAssessment(
            isoWeek.WeekNumber,
            isoWeek.Year,
            globalGoal);

        await _weeklyAssessmentRepository.AddAsync(
            weeklyAssessment,
            cancellationToken);

        var result = new List<StudyAreaWeekDto>(preparedItems.Count);

        foreach (var item in preparedItems)
        {
            var studyAreaWeek = new StudyAreaWeek(
                weekStartDate,
                item.StudyArea.Id,
                item.StudyPlan.Id,
                weeklyAssessment.Id);

            var assessment = new StudyAreaWeekAssessment(
                item.IndividualGoal,
                studyAreaWeek.Id);

            await _studyAreaWeekRepository.AddAsync(
                studyAreaWeek,
                cancellationToken);

            await _studyAreaWeekAssessmentRepository.AddAsync(
                assessment,
                cancellationToken);

            result.Add(new StudyAreaWeekDto(
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

    private static void ValidateRequest(BootstrapSystemCommand request)
    {
        if (request.Items is null)
        {
            throw new ArgumentException(
                "Items are required.",
                nameof(request.Items));
        }

        if (request.Items.Count == 0)
        {
            throw new ArgumentException(
                "At least one item is required.",
                nameof(request.Items));
        }

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                throw new ArgumentException(
                    "Items cannot contain null entries.",
                    nameof(request.Items));
            }

            if (item.StudyAreaId == Guid.Empty)
            {
                throw new ArgumentException(
                    "StudyAreaId is required.",
                    nameof(request.Items));
            }

            if (item.StudyPlanId == Guid.Empty)
            {
                throw new ArgumentException(
                    "StudyPlanId is required.",
                    nameof(request.Items));
            }
        }
    }

    private sealed record PreparedItem(
        StudyArea StudyArea,
        StudyPlan StudyPlan,
        decimal IndividualGoal);
}