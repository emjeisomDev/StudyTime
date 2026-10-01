using MediatR;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;

namespace StudyTime.Application.StudyAreaWeeks.Commands;

public sealed class CreateStudyAreaWeekHandler(
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyAreaWeekRepository studyAreaWeekRepository,
    IStudyAreaWeekAssessmentRepository studyAreaWeekAssessmentRepository,
    IWeeklyAssessmentRepository weeklyAssessmentRepository,
    ICurrentWeekProvider currentWeekProvider,
    IIsoWeekCalendar isoWeekCalendar,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStudyAreaWeekCommand, StudyAreaWeekDto>
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

    public async Task<StudyAreaWeekDto> Handle(CreateStudyAreaWeekCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIdentifiers(request);

        var currentWeek = _currentWeekProvider.GetCurrentWeek();
        var currentWeekStart = DateOnly.FromDateTime(currentWeek.Start);
        var lastAllowedWeekStart = currentWeekStart.AddDays(28);

        if (request.WeekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException("The configured week must start on a Monday.");
        }

        if (request.WeekStartDate < currentWeekStart || request.WeekStartDate > lastAllowedWeekStart)
        {
            throw new DomainException(
                "The configured week must be the current week or one of " +
                "the four following weeks.");
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
            throw new DomainException("An inactive StudyPlan cannot be used in a new weekly configuration.");
        }

        var alreadyExists = await _studyAreaWeekRepository.ExistsForStudyAreaInWeekAsync(
                request.StudyAreaId,
                request.WeekStartDate,
                cancellationToken);

        if (alreadyExists)
        {
            throw new DuplicateStudyAreaWeekException();
        }

        var individualGoal = GoalCalculator.CalculateIndividualGoal(
            studyArea.StdWeekStudyTime.Value,
            studyPlan.Coefficient.Value);

        var existingStudyAreaWeeks = await _studyAreaWeekRepository.GetByWeekStartDateAsync(
                request.WeekStartDate,
                cancellationToken);

        var individualGoals = new List<decimal>(existingStudyAreaWeeks.Count + 1)
        {
            individualGoal
        };

        foreach (var existingStudyAreaWeek in existingStudyAreaWeeks)
        {
            var assessment = await _studyAreaWeekAssessmentRepository
                    .GetByStudyAreaWeekIdAsync(
                        existingStudyAreaWeek.Id,
                        cancellationToken);

            if (assessment is null)
            {
                throw new DomainException(
                    $"StudyAreaWeekAssessment was not found for " +
                    $"StudyAreaWeek '{existingStudyAreaWeek.Id}'.");
            }

            individualGoals.Add(assessment.WeekIndividualGoal);
        }

        var globalGoal = GoalCalculator.CalculateGlobalGoal(individualGoals);

        WeeklyGoalValidator.Validate(globalGoal);

        var isoWeek = _isoWeekCalendar.GetIsoWeek(request.WeekStartDate);

        var weeklyAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (weeklyAssessment is null)
        {
            weeklyAssessment = new WeeklyAssessment(isoWeek.WeekNumber, isoWeek.Year, globalGoal);
            await _weeklyAssessmentRepository.AddAsync(weeklyAssessment, cancellationToken);
        }
        else
        {
            weeklyAssessment.UpdateGlobalGoal(globalGoal);
            _weeklyAssessmentRepository.Update(weeklyAssessment);
        }

        var studyAreaWeek = new StudyAreaWeek(
            request.WeekStartDate,
            studyArea.Id,
            studyPlan.Id,
            weeklyAssessment.Id);

        var studyAreaWeekAssessment = new StudyAreaWeekAssessment(individualGoal, studyAreaWeek.Id);
        await _studyAreaWeekRepository.AddAsync(studyAreaWeek, cancellationToken);
        await _studyAreaWeekAssessmentRepository.AddAsync(studyAreaWeekAssessment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudyAreaWeekDto(
            studyAreaWeek.Id,
            studyAreaWeek.WeekStartDate,
            studyAreaWeek.StudyAreaId,
            studyAreaWeek.StudyPlanId,
            studyAreaWeek.WeeklyAssessmentId,
            studyAreaWeekAssessment.WeekIndividualGoal,
            studyAreaWeekAssessment.MinutesStudied,
            GoalAchievedEvaluator.EvaluateIndividual(studyAreaWeekAssessment));
    }

    private static void ValidateIdentifiers(CreateStudyAreaWeekCommand request)
    {
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

        var currentAssessment = await _weeklyAssessmentRepository.GetByYearAndWeekNumberAsync(
                isoWeek.Year,
                isoWeek.WeekNumber,
                cancellationToken);

        if (currentAssessment is null || currentAssessment.MinutesStudied < currentAssessment.WeekGlobalGoal)
        {
            throw new WeekConfigurationLockedException(
                "Weekly configuration can only be changed after the " +
                "current week's global goal has been reached.");
        }
    }
}