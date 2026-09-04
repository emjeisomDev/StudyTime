using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

public sealed class UpdateSuccessTests
{
    [Fact]
    public async Task UpdateShouldChangeStudyAreaAndRecalculateIndividualAndGlobalGoals()
    {
        var week = new DateOnly(2026, 8, 31);
        var oldArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Java", 1800);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, oldArea, plan, weeklyAssessment.Id, 1500m);
        var record = TestHelpers.CreateStudyRecord(week, studyAreaWeek.Id, 1500);

        var repository = new FakeStudyAreaWeekRepository([studyAreaWeek], weeklyAssessment, [record]);
        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(oldArea, newArea),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(week),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(newArea.Id, null),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(studyAreaWeek.Id, result.Id);
        Assert.Equal(newArea.Id, studyAreaWeek.StudyAreaId);
        Assert.Equal(plan.Id, studyAreaWeek.StudyPlanId);
        Assert.Equal(1800m, studyAreaWeek.Assessment.WeekIndividualGoal);
        Assert.Equal(1800m, weeklyAssessment.WeekGlobalGoal);
        Assert.Equal(1500, studyAreaWeek.Assessment.MinutesStudied);
        Assert.Equal(1500, weeklyAssessment.MinutesStudied);
    }

    [Fact]
    public async Task UpdateShouldChangeStudyPlanAndRecalculateIndividualAndGlobalGoals()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, area, oldPlan, assessment.Id, 1500m);
        var record = TestHelpers.CreateStudyRecord(week, studyAreaWeek.Id, 1500);

        var repository = new FakeStudyAreaWeekRepository([studyAreaWeek], assessment, [record]);
        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(null, newPlan.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(studyAreaWeek.Id, result.Id);
        Assert.Equal(area.Id, studyAreaWeek.StudyAreaId);
        Assert.Equal(newPlan.Id, studyAreaWeek.StudyPlanId);
        Assert.Equal(1800m, studyAreaWeek.Assessment.WeekIndividualGoal);
        Assert.Equal(1800m, assessment.WeekGlobalGoal);
        Assert.Equal(1500, studyAreaWeek.Assessment.MinutesStudied);
        Assert.Equal(1500, assessment.MinutesStudied);
    }

    [Fact]
    public async Task UpdateShouldChangeBothStudyAreaAndStudyPlan()
    {
        var week = new DateOnly(2026, 8, 31);
        var oldArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Java", 1800);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);
        var assessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, oldArea, oldPlan, assessment.Id, 1500m);
        var record = TestHelpers.CreateStudyRecord(week, studyAreaWeek.Id, 1500);

        var repository = new FakeStudyAreaWeekRepository([studyAreaWeek], assessment, [record]);
        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(oldArea, newArea),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(newArea.Id, newPlan.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newArea.Id, studyAreaWeek.StudyAreaId);
        Assert.Equal(newPlan.Id, studyAreaWeek.StudyPlanId);
        Assert.Equal(2160m, studyAreaWeek.Assessment.WeekIndividualGoal);
        Assert.Equal(2160m, assessment.WeekGlobalGoal);
        Assert.Equal(1500, studyAreaWeek.Assessment.MinutesStudied);
        Assert.Equal(1500, assessment.MinutesStudied);
    }

    [Fact]
    public async Task UpdateShouldRecalculateMinutesFromExistingStudyRecords()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, area, plan, weeklyAssessment.Id, 1500m);

        // Pré-condição obrigatória: a meta global da semana corrente
        // precisa estar atingida antes da alteração.
        var record = TestHelpers.CreateStudyRecord(week, studyAreaWeek.Id, 1500);

        var repository = new FakeStudyAreaWeekRepository([studyAreaWeek], weeklyAssessment, [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(week),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(null, plan.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1500, studyAreaWeek.Assessment.MinutesStudied);
        Assert.Equal(1500, weeklyAssessment.MinutesStudied);
    }

    [Fact]
    public async Task UpdateShouldPreserveExistingStudyRecordsAndTheirStudyAreaWeekId()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Java", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, area, plan, weeklyAssessment.Id, 1500m);

        var record = StudyRecord.Create(
            Guid.NewGuid(),
            week.AddDays(1),
            DateTimeOffset.UtcNow,
            1500,
            studyAreaWeek.Id,
            week);

        var originalRecordId = record.Id;
        var originalStudyAreaWeekId = record.StudyAreaWeekId;
        var originalMinutes = record.Minutes;



        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area, newArea),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(week),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(newArea.Id, null),
            CancellationToken.None);

        Assert.NotNull(result);

        var persistedRecord = Assert.Single(repository.StudyRecords);
        Assert.Equal(originalRecordId, persistedRecord.Id);
        Assert.Equal(originalStudyAreaWeekId, persistedRecord.StudyAreaWeekId);
        Assert.Equal(originalMinutes, persistedRecord.Minutes);
        Assert.Equal(newArea.Id, studyAreaWeek.StudyAreaId);
        Assert.Equal(plan.Id, studyAreaWeek.StudyPlanId);
    }

    [Fact]
    public async Task UpdateShouldRecalculateGlobalGoalFromAllStudyAreaWeeks()
    {
        var week = new DateOnly(2026, 8, 31);
        var area1 = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var area2 = StudyArea.Create(Guid.NewGuid(), "Java", 1500);
        var newArea = StudyArea.Create(Guid.NewGuid(), "Python", 1800);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 3000m);

        var first = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area1,
            plan,
            weeklyAssessment.Id,
            1500m);

        var second = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area2,
            plan,
            weeklyAssessment.Id,
            1500m);

        var record1 = StudyRecord.Create(
            Guid.NewGuid(),
            week.AddDays(1),
            DateTimeOffset.UtcNow.AddMinutes(-2),
            1500,
            first.Id,
            week);

        var record2 = StudyRecord.Create(
            Guid.NewGuid(),
            week.AddDays(2),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            1500,
            second.Id,
            week);

        var repository = new FakeStudyAreaWeekRepository(
            [first, second],
            weeklyAssessment,
            [record1, record2]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area1, area2, newArea),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(week),
            new FakeUnitOfWork());

        var result = await service.UpdateAsync(
            first.Id,
            new UpdateStudyAreaWeekRequest(newArea.Id, null),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newArea.Id, first.StudyAreaId);
        Assert.Equal(plan.Id, first.StudyPlanId);
        Assert.Equal(1800m, first.Assessment.WeekIndividualGoal);
        Assert.Equal(3300m, weeklyAssessment.WeekGlobalGoal);
        Assert.Equal(1500, first.Assessment.MinutesStudied);
        Assert.Equal(1500, second.Assessment.MinutesStudied);
        Assert.Equal(3000, weeklyAssessment.MinutesStudied);
    }

    [Fact]
    public async Task UpdateShouldPersistAndCommitTransaction()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), week, area, plan, weeklyAssessment.Id, 1500m);
        var record = TestHelpers.CreateStudyRecord(week, studyAreaWeek.Id, 1500);
        var unitOfWork = new FakeUnitOfWork();

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(week),
            unitOfWork);

        var result = await service.UpdateAsync(
            studyAreaWeek.Id,
            new UpdateStudyAreaWeekRequest(null, plan.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
        Assert.Equal(0, unitOfWork.Transaction.RollbackCalls);
    }
}