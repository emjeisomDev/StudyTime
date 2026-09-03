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
        var currentAssessment = await GetWeeklyAssessmentAsync(currentWeekStartDate, cancellationToken);

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
            var weeklyAssessment = await GetWeeklyAssessmentAsync(request.WeekStartDate, cancellationToken);

            if (weeklyAssessment is null)
            {
                var (isoYear, isoWeek) = GetIsoWeek(request.WeekStartDate);
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

            return ToResponse(studyAreaWeek, weeklyAssessment);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StudyAreaWeekBatchResponse> CreateBatchAsync(CreateStudyAreaWeekBatchRequest request, CancellationToken cancellationToken)
    {
        ValidateBatchRequest(request);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            ValidateWeek(request.WeekStartDate);

            if (!calendar.IsWithinConfigurationWindow(request.WeekStartDate))
                throw new InvalidOperationException("The requested week is outside the allowed configuration window.");

            var targetWeek = calendar.GetWeek(request.WeekStartDate);
            var currentWeek = calendar.CurrentWeek;

            if (targetWeek.Equals(currentWeek))
                throw new InvalidOperationException("Manual creation of the current week is not allowed by the temporal configuration rule.");

            var currentWeekStartDate = currentWeek.WeekStartDate;
            var currentAssessment = await GetWeeklyAssessmentAsync(currentWeekStartDate, cancellationToken);

            if (currentAssessment is null || !await IsCurrentWeekGoalAchievedAsync(currentAssessment, currentWeekStartDate, cancellationToken))
                throw new InvalidOperationException("The current week's global goal must be achieved before changing the weekly configuration.");

            var duplicateAreaIds = request.Items
                .GroupBy(x => x.StudyAreaId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (duplicateAreaIds.Length > 0)
                throw new InvalidOperationException("The same StudyArea cannot appear more than once in the batch for the requested week.");

            var existingConfigurations = await repository.ListByWeekAsync(request.WeekStartDate, cancellationToken);
            var existingAreaIds = existingConfigurations
                .Select(x => x.StudyAreaId)
                .ToHashSet();

            var conflictingAreaIds = request.Items
                .Select(x => x.StudyAreaId)
                .Where(existingAreaIds.Contains)
                .Distinct()
                .ToArray();

            if (conflictingAreaIds.Length > 0)
                throw new InvalidOperationException("One or more StudyAreas already have a configuration for the requested week.");

            var calculatedItems = new List<CalculatedBatchItem>(request.Items.Count);

            foreach (var item in request.Items)
            {
                if (item.StudyAreaId == Guid.Empty)
                    throw new ArgumentException("StudyAreaId must not be empty.", nameof(request));

                if (item.StudyPlanId == Guid.Empty)
                    throw new ArgumentException("StudyPlanId must not be empty.", nameof(request));

                var studyArea = await studyAreaRepository.GetByIdAsync(item.StudyAreaId, cancellationToken);
                if (studyArea is null)
                    throw new KeyNotFoundException($"StudyArea '{item.StudyAreaId}' was not found.");

                var studyPlan = await studyPlanRepository.GetByIdAsync(item.StudyPlanId, cancellationToken);
                if (studyPlan is null)
                    throw new KeyNotFoundException($"StudyPlan '{item.StudyPlanId}' was not found.");

                if (studyPlan.Status != StudyPlanStatus.Active)
                    throw new InvalidOperationException($"The selected StudyPlan '{item.StudyPlanId}' must be active.");

                var individualGoal = CalculateIndividualGoal(studyArea.StdWeekStudyTime, studyPlan.Coefficient);
                calculatedItems.Add(new CalculatedBatchItem(item, studyArea, studyPlan, individualGoal));
            }

            var globalGoal = existingConfigurations.Sum(x => x.Assessment.WeekIndividualGoal)
                + calculatedItems.Sum(x => x.IndividualGoal);

            if (globalGoal < MinimumWeeklyGoal)
                throw new InvalidOperationException($"The resulting weekly goal must be at least {MinimumWeeklyGoal.ToString(CultureInfo.InvariantCulture)} minutes.");

            var weeklyAssessment = await GetWeeklyAssessmentAsync(request.WeekStartDate, cancellationToken);

            if (weeklyAssessment is null)
            {
                var (isoYear, isoWeek) = GetIsoWeek(request.WeekStartDate);
                weeklyAssessment = WeeklyAssessment.Create(isoYear, isoWeek, globalGoal);
                repository.AddWeeklyAssessment(weeklyAssessment);
            }
            else
            {
                weeklyAssessment.UpdateGlobalGoal(globalGoal);
            }

            var responses = new List<StudyAreaWeekResponse>(calculatedItems.Count);

            foreach (var calculatedItem in calculatedItems)
            {
                var studyAreaWeek = StudyAreaWeek.Create(
                    request.WeekStartDate,
                    calculatedItem.StudyArea,
                    calculatedItem.StudyPlan,
                    weeklyAssessment.Id,
                    calculatedItem.IndividualGoal);

                repository.Add(studyAreaWeek);
                responses.Add(ToResponse(studyAreaWeek, weeklyAssessment));
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new StudyAreaWeekBatchResponse(
                request.WeekStartDate,
                weeklyAssessment.Id,
                weeklyAssessment.WeekGlobalGoal,
                responses);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StudyAreaWeekAssessmentResponse?> GetAssessmentAsync(Guid studyAreaWeekId, CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
            throw new ArgumentException("StudyAreaWeekId must not be empty.", nameof(studyAreaWeekId));

        var studyAreaWeek = await repository.GetByIdAsync(studyAreaWeekId, cancellationToken);

        if (studyAreaWeek is null)
            return null;

        var assessment = studyAreaWeek.Assessment;

        if (assessment is null)
            throw new InvalidOperationException($"StudyAreaWeek '{studyAreaWeekId}' does not have a StudyAreaWeekAssessment.");

        return new StudyAreaWeekAssessmentResponse(
            studyAreaWeek.Id,
            assessment.WeekIndividualGoal,
            assessment.MinutesStudied,
            assessment.GoalAchieved);
    }

    public async Task<StudyAreaWeekResponse?> UpdateAsync(
        Guid studyAreaWeekId,
        UpdateStudyAreaWeekRequest request,
        CancellationToken cancellationToken)
    {
        ValidateUpdateRequest(studyAreaWeekId, request);

        var studyAreaWeek = await repository.GetByIdAsync(studyAreaWeekId, cancellationToken);

        if (studyAreaWeek is null)
            return null;

        if (!calendar.IsWithinConfigurationWindow(studyAreaWeek.WeekStartDate))
            throw new InvalidOperationException("The StudyAreaWeek is outside the allowed configuration window.");

        var currentWeek = calendar.CurrentWeek;
        var currentAssessment = await GetWeeklyAssessmentAsync(currentWeek.WeekStartDate, cancellationToken);

        if (currentAssessment is null ||
            !await IsCurrentWeekGoalAchievedAsync(currentAssessment, currentWeek.WeekStartDate, cancellationToken))
            throw new InvalidOperationException("The current week's global goal must be achieved before changing the weekly configuration.");

        var studyAreaId = request.StudyAreaId ?? studyAreaWeek.StudyAreaId;
        var studyPlanId = request.StudyPlanId ?? studyAreaWeek.StudyPlanId;

        var studyArea = await studyAreaRepository.GetByIdAsync(studyAreaId, cancellationToken);

        if (studyArea is null)
            throw new KeyNotFoundException($"StudyArea '{studyAreaId}' was not found.");

        var studyPlan = await studyPlanRepository.GetByIdAsync(studyPlanId, cancellationToken);

        if (studyPlan is null)
            throw new KeyNotFoundException($"StudyPlan '{studyPlanId}' was not found.");

        if (studyPlan.Status != StudyPlanStatus.Active)
            throw new InvalidOperationException($"The selected StudyPlan '{studyPlanId}' must be active.");

        var configurations = await repository.ListByWeekAsync(
            studyAreaWeek.WeekStartDate,
            cancellationToken);

        var conflictingConfiguration = configurations.Any(x =>
            x.Id != studyAreaWeek.Id &&
            x.StudyAreaId == studyAreaId);

        if (conflictingConfiguration)
            throw new InvalidOperationException("The StudyArea already has a configuration for the requested week.");

        var individualGoal = CalculateIndividualGoal(
            studyArea.StdWeekStudyTime,
            studyPlan.Coefficient);

        var globalGoal = configurations
            .Where(x => x.Id != studyAreaWeek.Id)
            .Sum(x => x.Assessment.WeekIndividualGoal) + individualGoal;

        if (globalGoal < MinimumWeeklyGoal)
            throw new InvalidOperationException(
                $"The resulting weekly goal must be at least {MinimumWeeklyGoal.ToString(CultureInfo.InvariantCulture)} minutes.");

        var weeklyAssessment = await GetWeeklyAssessmentAsync(
            studyAreaWeek.WeekStartDate,
            cancellationToken);

        if (weeklyAssessment is null)
            throw new InvalidOperationException("The WeeklyAssessment for the StudyAreaWeek was not found.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            studyAreaWeek.Reconfigure(
                studyArea,
                studyPlan,
                individualGoal);

            weeklyAssessment.UpdateGlobalGoal(globalGoal);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToResponse(studyAreaWeek, weeklyAssessment);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(
        DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        var (isoYear, isoWeek) = GetIsoWeek(weekStartDate);
        return await repository.GetWeeklyAssessmentAsync(
            isoYear,
            isoWeek,
            cancellationToken);
    }

    private async Task<bool> IsCurrentWeekGoalAchievedAsync(
        WeeklyAssessment assessment,
        DateOnly currentWeekStartDate,
        CancellationToken cancellationToken)
    {
        var configurations = await repository.ListByWeekAsync(
            currentWeekStartDate,
            cancellationToken);

        if (configurations.Count == 0)
            return false;

        return assessment.IsGoalAchieved(
            configurations.Select(x => x.Assessment));
    }

    private static decimal CalculateIndividualGoal(
        int standardWeeklyStudyTime,
        decimal coefficient)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            standardWeeklyStudyTime);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            coefficient);

        return standardWeeklyStudyTime * coefficient;
    }

    private static (int Year, int Week) GetIsoWeek(DateOnly weekStartDate)
    {
        var date = weekStartDate.ToDateTime(TimeOnly.MinValue);
        return (
            ISOWeek.GetYear(date),
            ISOWeek.GetWeekOfYear(date));
    }

    private static StudyAreaWeekResponse ToResponse(
        StudyAreaWeek studyAreaWeek,
        WeeklyAssessment weeklyAssessment)
    {
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

    private static void ValidateRequest(CreateStudyAreaWeekRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.StudyAreaId == Guid.Empty)
            throw new ArgumentException("StudyAreaId must not be empty.", nameof(request));

        if (request.StudyPlanId == Guid.Empty)
            throw new ArgumentException("StudyPlanId must not be empty.", nameof(request));

        ValidateWeek(request.WeekStartDate);
    }

    private static void ValidateBatchRequest(CreateStudyAreaWeekBatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null)
            throw new ArgumentNullException(
                nameof(request),
                "The batch Items collection must not be null.");

        if (request.Items.Count == 0)
            throw new ArgumentException(
                "The batch must contain at least one item.",
                nameof(request));

        ValidateWeek(request.WeekStartDate);
    }

    private static void ValidateUpdateRequest(
        Guid studyAreaWeekId,
        UpdateStudyAreaWeekRequest request)
    {
        if (studyAreaWeekId == Guid.Empty)
            throw new ArgumentException(
                "StudyAreaWeekId must not be empty.",
                nameof(studyAreaWeekId));

        ArgumentNullException.ThrowIfNull(request);

        if (request.StudyAreaId is null && request.StudyPlanId is null)
            throw new ArgumentException(
                "At least one of StudyAreaId or StudyPlanId must be provided.",
                nameof(request));

        if (request.StudyAreaId is Guid studyAreaId &&
            studyAreaId == Guid.Empty)
            throw new ArgumentException(
                "StudyAreaId must not be empty.",
                nameof(request));

        if (request.StudyPlanId is Guid studyPlanId &&
            studyPlanId == Guid.Empty)
            throw new ArgumentException(
                "StudyPlanId must not be empty.",
                nameof(request));
    }

    private static void ValidateWeek(DateOnly weekStartDate)
    {
        if (weekStartDate == default)
            throw new ArgumentException(
                "WeekStartDate is required.",
                nameof(weekStartDate));

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException(
                "WeekStartDate must be a Monday.",
                nameof(weekStartDate));
    }

    private sealed record CalculatedBatchItem(
        CreateStudyAreaWeekBatchItem Request,
        StudyArea StudyArea,
        StudyPlan StudyPlan,
        decimal IndividualGoal);
}