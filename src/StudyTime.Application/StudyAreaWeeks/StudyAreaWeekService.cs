using System.Globalization;
using StudyTime.Application.Common.Calendar;
using StudyTime.Application.Common.Transactions;
using StudyTime.Application.StudyAreas;
using StudyTime.Application.StudyPlans;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace StudyTime.Application.StudyAreaWeeks;

public sealed class StudyAreaWeekService(
    IStudyAreaWeekRepository repository,
    IStudyAreaRepository studyAreaRepository,
    IStudyPlanRepository studyPlanRepository,
    IApplicationCalendar calendar,
    IUnitOfWork unitOfWork)
    : IStudyAreaWeekService
{
    private const decimal MinimumWeeklyGoal = 1500m;

    public async Task<StudyAreaWeekResponse> CreateAsync(CreateStudyAreaWeekRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        if (!calendar.IsWithinConfigurationWindow(request.WeekStartDate))
            throw new InvalidOperationException("The requested week is outside the allowed configuration window.");

        var targetWeek = calendar.GetWeek(request.WeekStartDate);
        var currentWeek = calendar.CurrentWeek;

        if (targetWeek.Equals(currentWeek))
            throw new InvalidOperationException("Manual creation of the current week is not allowed by the temporal configuration rule.");

        var currentWeekStartDate = currentWeek.WeekStartDate;
        var currentAssessment = await repository.GetWeeklyAssessmentAsync(
            ISOWeek.GetYear(currentWeekStartDate.ToDateTime(TimeOnly.MinValue)),
            ISOWeek.GetWeekOfYear(currentWeekStartDate.ToDateTime(TimeOnly.MinValue)),
            cancellationToken);

        if (currentAssessment is null || !await IsCurrentWeekGoalAchievedAsync(currentAssessment, currentWeekStartDate, cancellationToken))
            throw new InvalidOperationException("The current week's global goal must be achieved before changing the weekly configuration.");

        var studyArea = await studyAreaRepository.GetByIdAsync(request.StudyAreaId, cancellationToken);

        if (studyArea is null)
            throw new KeyNotFoundException($"StudyArea '{request.StudyAreaId}' was not found.");

        var studyPlan = await studyPlanRepository.GetByIdAsync(request.StudyPlanId, cancellationToken);
        
        if (studyPlan is null)
            throw new KeyNotFoundException($"StudyPlan '{request.StudyPlanId}' was not found.");

        if (studyPlan.Status != StudyPlanStatus.Active)
            throw new InvalidOperationException("The selected StudyPlan must be active.");

        if (await repository.ExistsByAreaAndWeekAsync(request.StudyAreaId, request.WeekStartDate, cancellationToken))
            throw new InvalidOperationException("The StudyArea already has a configuration for the requested week.");

        var individualGoal = CalculateIndividualGoal(studyArea.StdWeekStudyTime, studyPlan.Coefficient);
        var existingConfigurations = await repository.ListByWeekAsync(request.WeekStartDate, cancellationToken);
        var globalGoal = existingConfigurations.Sum(x => x.Assessment.WeekIndividualGoal) + individualGoal;

        if (globalGoal < MinimumWeeklyGoal)
            throw new InvalidOperationException($"The resulting weekly goal must be at least {MinimumWeeklyGoal.ToString(CultureInfo.InvariantCulture)} minutes.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var isoDate = request.WeekStartDate.ToDateTime(TimeOnly.MinValue);
            var isoYear = ISOWeek.GetYear(isoDate);
            var isoWeek = ISOWeek.GetWeekOfYear(isoDate);

            var weeklyAssessment = await repository.GetWeeklyAssessmentAsync(isoYear, isoWeek, cancellationToken);

            if (weeklyAssessment is null)
            {
                weeklyAssessment = WeeklyAssessment.Create(isoYear, isoWeek, globalGoal);
                repository.AddWeeklyAssessment(weeklyAssessment);
            }
            else
            {
                weeklyAssessment.UpdateGlobalGoal(globalGoal);
            }

            var studyAreaWeek = StudyAreaWeek.Create(
                request.WeekStartDate,
                studyArea,
                studyPlan,
                weeklyAssessment.Id,
                individualGoal);

            repository.Add(studyAreaWeek);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new StudyAreaWeekResponse(
                studyAreaWeek.Id,
                studyAreaWeek.StudyAreaId,
                studyAreaWeek.StudyPlanId,
                studyAreaWeek.WeekStartDate,
                studyAreaWeek.WeeklyAssessmentId,
                studyAreaWeek.Assessment.WeekIndividualGoal,
                weeklyAssessment.WeekGlobalGoal,
                studyAreaWeek.Assessment.MinutesStudied);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> IsCurrentWeekGoalAchievedAsync(
        WeeklyAssessment assessment,
        DateOnly currentWeekStartDate,
        CancellationToken cancellationToken)
    {
        var configurations = await repository.ListByWeekAsync(currentWeekStartDate, cancellationToken);
        if (configurations.Count == 0)
            return false;

        return assessment.IsGoalAchieved(configurations.Select(x => x.Assessment));
    }

    private static decimal CalculateIndividualGoal(int standardWeeklyStudyTime, decimal coefficient)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(standardWeeklyStudyTime);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(coefficient);
        return standardWeeklyStudyTime * coefficient;
    }

    private static void ValidateRequest(CreateStudyAreaWeekRequest request)
    {
        if (request.StudyAreaId == Guid.Empty)
            throw new ArgumentException("StudyAreaId must not be empty.", nameof(request));

        if (request.StudyPlanId == Guid.Empty)
            throw new ArgumentException("StudyPlanId must not be empty.", nameof(request));

        if (request.WeekStartDate == default)
            throw new ArgumentException("WeekStartDate is required.", nameof(request));

        if (request.WeekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("WeekStartDate must be a Monday.", nameof(request));
    }
}