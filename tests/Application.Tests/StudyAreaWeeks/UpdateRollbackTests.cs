using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

public sealed class UpdateRollbackTests
{
    [Fact]
    public async Task UpdateShouldRollbackWhenSaveChangesFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork
        {
            SaveChangesException =
                new InvalidOperationException("Persistence failure.")
        };

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        Assert.Equal(
            "Persistence failure.",
            exception.Message);

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCalls);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCalls);

        Assert.Equal(
            0,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);

        Assert.False(
            unitOfWork.Transaction.IsCommitted);

        Assert.Equal(
            newPlan.Id,
            studyAreaWeek.StudyPlanId);

        Assert.Equal(
            1800m,
            studyAreaWeek.Assessment.WeekIndividualGoal);

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        var persistedRecord =
            Assert.Single(repository.StudyRecords);

        Assert.Equal(
            record.Id,
            persistedRecord.Id);

        Assert.Equal(
            record.StudyAreaWeekId,
            persistedRecord.StudyAreaWeekId);

        Assert.Equal(
            1500,
            persistedRecord.Minutes);
    }

    [Fact]
    public async Task UpdateShouldRollbackWhenCommitFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork();

        unitOfWork.Transaction.CommitException =
            new InvalidOperationException(
                "Commit failure.");

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        Assert.Equal(
            "Commit failure.",
            exception.Message);

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCalls);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);

        Assert.False(
            unitOfWork.Transaction.IsCommitted);

        Assert.Equal(
            newPlan.Id,
            studyAreaWeek.StudyPlanId);

        Assert.Equal(
            1800m,
            studyAreaWeek.Assessment.WeekIndividualGoal);

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        var persistedRecord =
            Assert.Single(repository.StudyRecords);

        Assert.Equal(
            record.Id,
            persistedRecord.Id);

        Assert.Equal(
            record.StudyAreaWeekId,
            persistedRecord.StudyAreaWeekId);

        Assert.Equal(
            1500,
            persistedRecord.Minutes);
    }

    [Fact]
    public async Task UpdateShouldNotCommitWhenSaveChangesFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork
        {
            SaveChangesException =
                new InvalidOperationException(
                    "SaveChanges failure.")
        };

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCalls);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCalls);

        Assert.Equal(
            0,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);

        Assert.False(
            unitOfWork.Transaction.IsCommitted);

        Assert.Equal(
            newPlan.Id,
            studyAreaWeek.StudyPlanId);

        Assert.Equal(
            1800m,
            studyAreaWeek.Assessment.WeekIndividualGoal);

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        var persistedRecord =
            Assert.Single(repository.StudyRecords);

        Assert.Equal(
            record.Id,
            persistedRecord.Id);

        Assert.Equal(
            record.StudyAreaWeekId,
            persistedRecord.StudyAreaWeekId);

        Assert.Equal(
            1500,
            persistedRecord.Minutes);
    }

    [Fact]
    public async Task UpdateShouldAttemptCommitBeforeRollbackWhenCommitFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork();

        unitOfWork.Transaction.CommitException =
            new InvalidOperationException(
                "Commit failure.");

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        Assert.Equal(
            1,
            unitOfWork.BeginTransactionCalls);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);

        Assert.False(
            unitOfWork.Transaction.IsCommitted);
    }

    [Fact]
    public async Task UpdateShouldIncludeWeeklyAssessmentInTransactionBeforeSaveChangesFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork
        {
            SaveChangesException =
                new InvalidOperationException(
                    "SaveChanges failure.")
        };

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        Assert.Equal(
            1500m,
            weeklyAssessment.MinutesStudied);

        Assert.Equal(
            1800m,
            studyAreaWeek.Assessment.WeekIndividualGoal);

        Assert.Equal(
            1500m,
            studyAreaWeek.Assessment.MinutesStudied);

        Assert.Equal(
            0,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);
    }

    [Fact]
    public async Task UpdateShouldIncludeWeeklyAssessmentInTransactionBeforeCommitFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork();

        unitOfWork.Transaction.CommitException =
            new InvalidOperationException(
                "Commit failure.");

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        Assert.Equal(
            1500m,
            weeklyAssessment.MinutesStudied);

        Assert.Equal(
            1800m,
            studyAreaWeek.Assessment.WeekIndividualGoal);

        Assert.Equal(
            1500m,
            studyAreaWeek.Assessment.MinutesStudied);

        Assert.Equal(
            1,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);
    }

    [Fact]
    public async Task UpdateShouldPreserveStudyRecordWhenSaveChangesFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var originalId = record.Id;
        var originalStudyAreaWeekId =
            record.StudyAreaWeekId;
        var originalMinutes = record.Minutes;

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var unitOfWork = new FakeUnitOfWork
        {
            SaveChangesException =
                new InvalidOperationException(
                    "Persistence failure.")
        };

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        var persistedRecord =
            Assert.Single(repository.StudyRecords);

        Assert.Equal(
            originalId,
            persistedRecord.Id);

        Assert.Equal(
            originalStudyAreaWeekId,
            persistedRecord.StudyAreaWeekId);

        Assert.Equal(
            originalMinutes,
            persistedRecord.Minutes);

        Assert.Equal(
            0,
            unitOfWork.Transaction.CommitCalls);

        Assert.Equal(
            1,
            unitOfWork.Transaction.RollbackCalls);
    }

    [Fact]
    public async Task UpdateShouldPreserveStudyRecordWhenCommitFails()
    {
        var week = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1.2m);

        var weeklyAssessment =
            TestHelpers.CreateAchievedCurrentWeekAssessment(week);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            week,
            area,
            oldPlan,
            weeklyAssessment.Id,
            1500m);

        var record = TestHelpers.CreateStudyRecord(
            week,
            studyAreaWeek.Id,
            1500);

        var originalId = record.Id;
        var originalStudyAreaWeekId =
            record.StudyAreaWeekId;
        var originalMinutes = record.Minutes;

        var repository = new FakeStudyAreaWeekRepository([studyAreaWeek], weeklyAssessment, [record]);

        var unitOfWork = new FakeUnitOfWork();

        unitOfWork.Transaction.CommitException = new InvalidOperationException("Commit failure.");

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(oldPlan, newPlan),
            new FixedCalendar(week),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, newPlan.Id),
                CancellationToken.None));

        var persistedRecord = Assert.Single(repository.StudyRecords);

        Assert.Equal(originalId, persistedRecord.Id);

        Assert.Equal(originalStudyAreaWeekId, persistedRecord.StudyAreaWeekId);
        Assert.Equal(originalMinutes, persistedRecord.Minutes);
        Assert.Equal(1, unitOfWork.Transaction.CommitCalls);
        Assert.Equal(1, unitOfWork.Transaction.RollbackCalls);
    }
}