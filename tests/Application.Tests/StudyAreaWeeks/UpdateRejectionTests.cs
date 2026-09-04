using StudyTime.Application.StudyAreaWeeks;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreaWeeks;

public sealed class UpdateRejectionTests
{
    [Fact]
    public async Task UpdateShouldRejectWhenCurrentWeekGoalIsNotAchieved()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(Guid.NewGuid(), currentWeek, area, plan, weeklyAssessment.Id, 1500m);
        var record = TestHelpers.CreateStudyRecord(currentWeek, studyAreaWeek.Id, 1499);

        var repository = new FakeStudyAreaWeekRepository([studyAreaWeek], weeklyAssessment, [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(area.Id, null),
                CancellationToken.None));

        Assert.Contains(
            "current week's global goal must be achieved",
            exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectMissingStudyArea()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var existingArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var missingAreaId = Guid.NewGuid();
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            existingArea,
            plan,
            weeklyAssessment.Id,
            1500m);

        // Atinge a meta global da semana corrente para permitir
        // que a execução avance até a validação da StudyArea.
        var record = TestHelpers.CreateStudyRecord(
            currentWeek,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(existingArea),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(missingAreaId, null),
                CancellationToken.None));

        Assert.Contains(
            missingAreaId.ToString(),
            exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectMissingStudyPlan()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var currentPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var missingPlanId = Guid.NewGuid();
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            area,
            currentPlan,
            weeklyAssessment.Id,
            1500m);

        // A pré-condição global é satisfeita antes de testar
        // a inexistência do StudyPlan.
        var record = TestHelpers.CreateStudyRecord(
            currentWeek,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(currentPlan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, missingPlanId),
                CancellationToken.None));

        Assert.Contains(
            missingPlanId.ToString(),
            exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectInactiveStudyPlan()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var activePlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var inactivePlan = StudyPlan.Create(Guid.NewGuid(), "Inativo", 1m);
        inactivePlan.Deactivate();

        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 1500m);
        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            area,
            activePlan,
            weeklyAssessment.Id,
            1500m);

        // A meta global corrente precisa estar atingida para que
        // a validação do status do novo StudyPlan seja executada.
        var record = TestHelpers.CreateStudyRecord(
            currentWeek,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            weeklyAssessment,
            [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(activePlan, inactivePlan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(null, inactivePlan.Id),
                CancellationToken.None));

        Assert.Contains(
            "must be active",
            exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectStudyAreaAlreadyConfiguredForWeek()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var currentArea = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var conflictingArea = StudyArea.Create(Guid.NewGuid(), "Java", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var weeklyAssessment = WeeklyAssessment.Create(2026, 36, 3000m);

        var currentConfiguration = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            currentArea,
            plan,
            weeklyAssessment.Id,
            1500m);

        var conflictingConfiguration = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            conflictingArea,
            plan,
            weeklyAssessment.Id,
            1500m);

        // As duas configurações possuem suas metas individuais atingidas.
        // Assim a regra de meta global corrente é satisfeita antes
        // da verificação do conflito.
        var currentRecord = TestHelpers.CreateStudyRecord(
            currentWeek,
            currentConfiguration.Id,
            1500);

        var conflictingRecord = TestHelpers.CreateStudyRecord(
            currentWeek,
            conflictingConfiguration.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [currentConfiguration, conflictingConfiguration],
            weeklyAssessment,
            [currentRecord, conflictingRecord]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(
                currentArea,
                conflictingArea),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                currentConfiguration.Id,
                new UpdateStudyAreaWeekRequest(
                    conflictingArea.Id,
                    null),
                CancellationToken.None));

        Assert.Contains(
            "already has a configuration",
            exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectResultingGlobalGoalBelowMinimum()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 500);
        var oldPlan = StudyPlan.Create(Guid.NewGuid(), "Normal", 3m);
        var newPlan = StudyPlan.Create(Guid.NewGuid(), "Baixo", 1m);

        var currentAssessment = WeeklyAssessment.Create(
            2026,
            36,
            1500m);

        var studyAreaWeek = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            area,
            oldPlan,
            currentAssessment.Id,
            1500m);

        // A meta da semana corrente é atingida com 1500 minutos.
        // Depois disso, a atualização reduz a meta resultante
        // para 500 minutos, violando o mínimo de 1500.
        var record = TestHelpers.CreateStudyRecord(
            currentWeek,
            studyAreaWeek.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [studyAreaWeek],
            currentAssessment,
            [record]);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(
                oldPlan,
                newPlan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                studyAreaWeek.Id,
                new UpdateStudyAreaWeekRequest(
                    null,
                    newPlan.Id),
                CancellationToken.None));

        Assert.Contains(
            "at least 1500",
            exception.Message);
    }

    [Fact]
    public async Task UpdateShouldRejectWhenTargetWeeklyAssessmentDoesNotExist()
    {
        var currentWeek = new DateOnly(2026, 8, 31);
        var targetWeek = currentWeek.AddDays(7);

        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);

        var currentAssessment = WeeklyAssessment.Create(
            2026,
            36,
            1500m);

        var targetAssessment = WeeklyAssessment.Create(
            2026,
            37,
            1500m);

        var currentConfiguration = StudyAreaWeek.Create(
            Guid.NewGuid(),
            currentWeek,
            area,
            plan,
            currentAssessment.Id,
            1500m);

        var targetConfiguration = StudyAreaWeek.Create(
            Guid.NewGuid(),
            targetWeek,
            area,
            plan,
            targetAssessment.Id,
            1500m);

        // Somente a meta da semana corrente precisa estar atingida.
        // A WeeklyAssessment da semana alvo será removida do fake
        // para testar especificamente essa rejeição.
        var currentRecord = TestHelpers.CreateStudyRecord(
            currentWeek,
            currentConfiguration.Id,
            1500);

        var repository = new FakeStudyAreaWeekRepository(
            [currentConfiguration, targetConfiguration],
            currentAssessment,
            [currentRecord]);

        repository.SetWeeklyAssessment(
            targetWeek,
            null);

        var service = TestHelpers.CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(plan),
            new FixedCalendar(currentWeek),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                targetConfiguration.Id,
                new UpdateStudyAreaWeekRequest(
                    area.Id,
                    null),
                CancellationToken.None));

        Assert.Contains(
            "WeeklyAssessment for the StudyAreaWeek was not found",
            exception.Message);
    }
}