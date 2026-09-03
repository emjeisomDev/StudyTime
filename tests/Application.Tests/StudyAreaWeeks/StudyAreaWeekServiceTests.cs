using StudyTime.Application.Common.Calendar;
using StudyTime.Application.Common.Transactions;
using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Application.StudyAreas;
using StudyTime.Application.StudyPlans;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace Application.Tests.StudyAreaWeeks;

public sealed class StudyAreaWeekServiceTests
{
    [Fact]
    public async Task GetAssessmentShouldReturnIndividualAssessment()
    {
        var studyArea = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var studyPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), new DateOnly(2026, 9, 7), studyArea, studyPlan, weeklyAssessmentId, 1000m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1000);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek);
        var service = CreateService(repository);

        var result = await service.GetAssessmentAsync(studyAreaWeek.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(studyAreaWeek.Id, result.StudyAreaWeekId);
        Assert.Equal(1000m, result.WeekIndividualGoal);
        Assert.Equal(1000, result.MinutesStudied);
        Assert.True(result.GoalAchieved);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnFalseWhenIndividualGoalIsNotAchieved()
    {
        var studyArea = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var studyPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), new DateOnly(2026, 9, 7), studyArea, studyPlan, Guid.NewGuid(), 1000m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(999);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek);
        var service = CreateService(repository);

        var result = await service.GetAssessmentAsync(studyAreaWeek.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1000m, result.WeekIndividualGoal);
        Assert.Equal(999, result.MinutesStudied);
        Assert.False(result.GoalAchieved);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnNullWhenStudyAreaWeekDoesNotExist()
    {
        var repository = new FakeStudyAreaWeekRepository((StudyAreaWeek?)null);
        var service = CreateService(repository);

        var result = await service.GetAssessmentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssessmentShouldRejectEmptyStudyAreaWeekId()
    {
        var repository = new FakeStudyAreaWeekRepository((StudyAreaWeek?)null);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetAssessmentAsync(Guid.Empty, CancellationToken.None));

        Assert.Contains("StudyAreaWeekId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldChangeStudyAreaAndRecalculateIndividualAndGlobalGoals()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var oldArea = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Java", 1200);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.5m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var currentConfiguration = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, oldArea, oldPlan, weeklyAssessment.Id, 1500m);
        currentConfiguration.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository(currentConfiguration, weeklyAssessment);
        var areaRepository = new FakeStudyAreaRepository(oldArea, newArea);
        var planRepository = new FakeStudyPlanRepository(oldPlan, newPlan);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, areaRepository, planRepository, new FixedCalendar(currentWeek), unitOfWork);

        var result = await service.UpdateAsync(currentConfiguration.Id, new UpdateStudyAreaWeekRequest(newArea.Id, newPlan.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newArea.Id, result.StudyAreaId);
        Assert.Equal(newPlan.Id, result.StudyPlanId);
        Assert.Equal(1800m, result.WeekIndividualGoal);
        Assert.Equal(1800m, result.WeekGlobalGoal);
        Assert.Equal(1500, result.MinutesStudied);
        Assert.Equal(newArea.Id, currentConfiguration.StudyAreaId);
        Assert.Equal(newPlan.Id, currentConfiguration.StudyPlanId);
        Assert.Equal(1800m, currentConfiguration.Assessment.WeekIndividualGoal);
        Assert.Equal(1800m, weeklyAssessment.WeekGlobalGoal);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
        Assert.Equal(0, unitOfWork.Transaction.RollbackCalls);
    }

    [Fact]
    public async Task UpdateShouldChangeOnlyStudyAreaWhenStudyPlanIsOmitted()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var oldArea = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Java", 1600);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, oldArea, plan, weeklyAssessment.Id, 1000m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1000);

        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var areaRepository = new FakeStudyAreaRepository(oldArea, newArea);
        var planRepository = new FakeStudyPlanRepository(plan);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, areaRepository, planRepository, new FixedCalendar(currentWeek), unitOfWork);

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(newArea.Id, null),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newArea.Id, result.StudyAreaId);
        Assert.Equal(plan.Id, result.StudyPlanId);
        Assert.Equal(1600m, result.WeekIndividualGoal);
        Assert.Equal(1600m, result.WeekGlobalGoal);
        Assert.Equal(1000, result.MinutesStudied);
        Assert.Equal(newArea.Id, studyAreaWeek.StudyAreaId);
        Assert.Equal(plan.Id, studyAreaWeek.StudyPlanId);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
    }

    [Fact]
    public async Task UpdateShouldChangeOnlyStudyPlanWhenStudyAreaIsOmitted()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.5m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1000m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, oldPlan, weeklyAssessment.Id, 1000m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1000);

        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var areaRepository = new FakeStudyAreaRepository(area);
        var planRepository = new FakeStudyPlanRepository(oldPlan, newPlan);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, areaRepository, planRepository, new FixedCalendar(currentWeek), unitOfWork);

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(null, newPlan.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(area.Id, result.StudyAreaId);
        Assert.Equal(newPlan.Id, result.StudyPlanId);
        Assert.Equal(1500m, result.WeekIndividualGoal);
        Assert.Equal(1500m, result.WeekGlobalGoal);
        Assert.Equal(1000, result.MinutesStudied);
        Assert.Equal(area.Id, studyAreaWeek.StudyAreaId);
        Assert.Equal(newPlan.Id, studyAreaWeek.StudyPlanId);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
    }

    [Fact]
    public async Task UpdateShouldReturnNullWhenStudyAreaWeekDoesNotExist()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var repository = new FakeStudyAreaWeekRepository((StudyAreaWeek?)null);
        var service = CreateService(repository, new FakeStudyAreaRepository(), new FakeStudyPlanRepository(), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateStudyAreaWeekRequest(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateShouldRejectEmptyStudyAreaWeekId()
    {
        var service = CreateService(new FakeStudyAreaWeekRepository((StudyAreaWeek?)null));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.Empty, new UpdateStudyAreaWeekRequest(Guid.NewGuid(), null), CancellationToken.None));

        Assert.Contains("StudyAreaWeekId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectRequestWithoutStudyAreaOrStudyPlan()
    {
        var service = CreateService(new FakeStudyAreaWeekRepository((StudyAreaWeek?)null));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), new UpdateStudyAreaWeekRequest(null, null), CancellationToken.None));

        Assert.Contains("At least one", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectEmptyStudyAreaId()
    {
        var service = CreateService(new FakeStudyAreaWeekRepository((StudyAreaWeek?)null));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), new UpdateStudyAreaWeekRequest(Guid.Empty, null), CancellationToken.None));

        Assert.Contains("StudyAreaId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectEmptyStudyPlanId()
    {
        var service = CreateService(new FakeStudyAreaWeekRepository((StudyAreaWeek?)null));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), new UpdateStudyAreaWeekRequest(null, Guid.Empty), CancellationToken.None));

        Assert.Contains("StudyPlanId", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectStudyAreaWeekOutsideConfigurationWindow()
    {
        var targetWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), targetWeek, area, plan, weeklyAssessment.Id, 1500m);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(plan), new FixedCalendar(targetWeek, false), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(area.Id, null), CancellationToken.None));

        Assert.Contains("outside the allowed configuration window", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectWhenCurrentWeekGoalIsNotAchieved()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var currentConfiguration = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, plan, weeklyAssessment.Id, 1500m);
        currentConfiguration.Assessment.UpdateMinutesStudied(1499);
        var repository = new FakeStudyAreaWeekRepository(currentConfiguration, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(currentConfiguration.Id, new UpdateStudyAreaWeekRequest(area.Id, null), CancellationToken.None));

        Assert.Contains("current week's global goal must be achieved", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectWhenCurrentWeekAssessmentDoesNotExist()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, plan, Guid.NewGuid(), 1500m);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, (WeeklyAssessment?)null);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(area.Id, null), CancellationToken.None));

        Assert.Contains("current week's global goal must be achieved", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectMissingStudyArea()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var existingArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var missingAreaId = Guid.NewGuid();
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, existingArea, plan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(existingArea), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(missingAreaId, null), CancellationToken.None));

        Assert.Contains(missingAreaId.ToString(), exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectMissingStudyPlan()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var existingArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var missingPlanId = Guid.NewGuid();
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, existingArea, plan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(existingArea), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(null, missingPlanId), CancellationToken.None));

        Assert.Contains(missingPlanId.ToString(), exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectInactiveStudyPlan()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var activePlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var inactivePlan = StudyPlan.Create(Guid.NewGuid(), "Inativo", 1m, StudyPlanStatus.Inactive);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, activePlan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(activePlan, inactivePlan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(null, inactivePlan.Id), CancellationToken.None));

        Assert.Contains("must be active", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectStudyAreaAlreadyConfiguredForWeek()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var currentArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var conflictingArea = StudyArea.Create(Guid.NewGuid(), "Java", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 3000m);
        var currentConfiguration = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, currentArea, plan, weeklyAssessment.Id, 1500m);
        var conflictingConfiguration = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, conflictingArea, plan, weeklyAssessment.Id, 1500m);
        currentConfiguration.Assessment.UpdateMinutesStudied(1500);
        conflictingConfiguration.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository([currentConfiguration, conflictingConfiguration], weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(currentArea, conflictingArea), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(currentConfiguration.Id, new UpdateStudyAreaWeekRequest(conflictingArea.Id, null), CancellationToken.None));

        Assert.Contains("already has a configuration", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectResultingGlobalGoalBelowMinimum()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 3m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Baixo", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, oldPlan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(oldPlan, newPlan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(null, newPlan.Id), CancellationToken.None));

        Assert.Contains("at least 1500", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectWhenWeeklyAssessmentForTargetWeekDoesNotExist()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var targetWeek = currentWeek.AddDays(7);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var currentAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var targetConfiguration = StudyAreaWeek.Create(Guid.NewGuid(), targetWeek, area, plan, Guid.NewGuid(), 1500m);
        var currentConfiguration = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, plan, currentAssessment.Id, 1500m);
        currentConfiguration.Assessment.UpdateMinutesStudied(1500);
        var repository = new FakeStudyAreaWeekRepository([targetConfiguration, currentConfiguration], currentAssessment);
        repository.SetWeeklyAssessment(targetWeek, null);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(targetConfiguration.Id, new UpdateStudyAreaWeekRequest(area.Id, null), CancellationToken.None));

        Assert.Contains("WeeklyAssessment for the StudyAreaWeek was not found", exception.Message);
    }

    [Fact]
    public async Task UpdateShouldPersistAndCommitTransaction()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, plan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var unitOfWork = new FakeUnitOfWork();
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(plan), new FixedCalendar(currentWeek), unitOfWork);

        await service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(null, plan.Id), CancellationToken.None);

        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
        Assert.Equal(0, unitOfWork.Transaction.RollbackCalls);
    }

    [Fact]
    public async Task UpdateShouldRollbackWhenSaveChangesFails()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, oldPlan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var unitOfWork = new FakeUnitOfWork
        {
            SaveChangesException = new InvalidOperationException("Persistence failure.")
        };
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(oldPlan, newPlan), new FixedCalendar(currentWeek), unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(null, newPlan.Id), CancellationToken.None));

        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(0, unitOfWork.Transaction.CommitCalls);
        Assert.Equal(1, unitOfWork.Transaction.RollbackCalls);
    }

    [Fact]
    public async Task UpdateShouldRollbackWhenCommitFails()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, oldPlan, weeklyAssessment.Id, 1500m);
        studyAreaWeek.Assessment.UpdateMinutesStudied(1500);
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.Transaction.CommitException = new InvalidOperationException("Commit failure.");
        var repository = new FakeStudyAreaWeekRepository(studyAreaWeek, weeklyAssessment);
        var service = CreateService(repository, new FakeStudyAreaRepository(area), new FakeStudyPlanRepository(oldPlan, newPlan), new FixedCalendar(currentWeek), unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(studyAreaWeek.Id, new UpdateStudyAreaWeekRequest(null, newPlan.Id), CancellationToken.None));

        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
        Assert.Equal(1, unitOfWork.Transaction.RollbackCalls);
    }

    private static StudyAreaWeekService CreateService(
        FakeStudyAreaWeekRepository repository,
        FakeStudyAreaRepository? studyAreaRepository = null,
        FakeStudyPlanRepository? studyPlanRepository = null,
        IApplicationCalendar? calendar = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new StudyAreaWeekService(
            repository,
            studyAreaRepository ?? new FakeStudyAreaRepository(),
            studyPlanRepository ?? new FakeStudyPlanRepository(),
            calendar ?? new FixedCalendar(new DateOnly(2026, 8, 31)),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private sealed class FakeStudyAreaWeekRepository : IStudyAreaWeekRepository
    {
        private readonly List<StudyAreaWeek> _studyAreaWeeks;
        private readonly Dictionary<(int Year, int Week), WeeklyAssessment?> _weeklyAssessments = [];

        public FakeStudyAreaWeekRepository(StudyAreaWeek? studyAreaWeek, WeeklyAssessment? weeklyAssessment = null)
            : this(studyAreaWeek is null ? [] : [studyAreaWeek], weeklyAssessment)
        {
        }

        public FakeStudyAreaWeekRepository(IReadOnlyList<StudyAreaWeek> studyAreaWeeks, WeeklyAssessment? weeklyAssessment = null)
        {
            _studyAreaWeeks = studyAreaWeeks.ToList();

            if (weeklyAssessment is not null)
                _weeklyAssessments[(weeklyAssessment.Year, weeklyAssessment.WeekNumber)] = weeklyAssessment;
        }

        public Task<StudyAreaWeek?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_studyAreaWeeks.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken)
        {
            IReadOnlyList<StudyAreaWeek> result = _studyAreaWeeks.Where(x => x.WeekStartDate == weekStartDate).ToList();
            return Task.FromResult(result);
        }

        public Task<bool> ExistsByAreaAndWeekAsync(Guid studyAreaId, DateOnly weekStartDate, CancellationToken cancellationToken)
            => Task.FromResult(_studyAreaWeeks.Any(x => x.StudyAreaId == studyAreaId && x.WeekStartDate == weekStartDate));

        public Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(int year, int weekNumber, CancellationToken cancellationToken)
            => Task.FromResult(_weeklyAssessments.TryGetValue((year, weekNumber), out var assessment) ? assessment : null);

        public void Add(StudyAreaWeek studyAreaWeek)
            => _studyAreaWeeks.Add(studyAreaWeek);

        public void AddWeeklyAssessment(WeeklyAssessment weeklyAssessment)
            => _weeklyAssessments[(weeklyAssessment.Year, weeklyAssessment.WeekNumber)] = weeklyAssessment;

        public void SetWeeklyAssessment(DateOnly weekStartDate, WeeklyAssessment? assessment)
        {
            var date = weekStartDate.ToDateTime(TimeOnly.MinValue);
            var key = (System.Globalization.ISOWeek.GetYear(date), System.Globalization.ISOWeek.GetWeekOfYear(date));
            _weeklyAssessments[key] = assessment;
        }
    }

    private sealed class FakeStudyAreaRepository(params StudyArea[] studyAreas) : IStudyAreaRepository
    {
        private readonly List<StudyArea> _studyAreas = studyAreas.ToList();

        public Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_studyAreas.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<StudyArea>> ListAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<StudyArea> result = _studyAreas;
            return Task.FromResult(result);
        }

        public Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
            => Task.FromResult(_studyAreas.Any(x => x.Name == name && x.Id != excludedId));

        public Task<bool> HasDependenciesAsync(Guid studyAreaId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public void Add(StudyArea studyArea)
            => _studyAreas.Add(studyArea);

        public void Remove(StudyArea studyArea)
            => _studyAreas.Remove(studyArea);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeStudyPlanRepository(params StudyPlan[] studyPlans) : IStudyPlanRepository
    {
        private readonly List<StudyPlan> _studyPlans = studyPlans.ToList();

        public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_studyPlans.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<StudyPlan>> ListAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<StudyPlan> result = _studyPlans;
            return Task.FromResult(result);
        }

        public void Add(StudyPlan studyPlan)
            => _studyPlans.Add(studyPlan);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FixedCalendar(DateOnly currentWeekStartDate, bool withinWindow = true) : IApplicationCalendar
    {
        public ApplicationWeek CurrentWeek => new(currentWeekStartDate);
        public ApplicationWeek PreviousWeek => CurrentWeek.AddWeeks(-1);
        public ApplicationWeek NextWeek => CurrentWeek.AddWeeks(1);
        public IReadOnlyList<ApplicationWeek> ConfigurationWeeks => [CurrentWeek, NextWeek];

        public ApplicationWeek GetWeek(DateOnly dateWeek)
        {
            var daysSinceMonday = ((int)dateWeek.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return new ApplicationWeek(dateWeek.AddDays(-daysSinceMonday));
        }

        public bool IsWithinConfigurationWindow(DateOnly weekStartDate)
            => withinWindow;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int BeginTransactionCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }
        public Exception? SaveChangesException { get; init; }
        public FakeTransaction Transaction { get; } = new();

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginTransactionCalls++;
            return Task.FromResult<ITransaction>(Transaction);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;

            if (SaveChangesException is not null)
                throw SaveChangesException;

            return Task.FromResult(1);
        }
    }

    private sealed class FakeTransaction : ITransaction
    {
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }
        public Exception? CommitException { get; set; }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCalls++;

            if (CommitException is not null)
                throw CommitException;

            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalls++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}