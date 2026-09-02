using System.Globalization;
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
    private static readonly DateOnly CurrentWeekStart = new(2026, 8, 31);
    private static readonly DateOnly TargetWeekStart = new(2026, 9, 7);

    [Fact]
    public async Task CreateShouldRejectNonMonday()
    {
        var service = CreateService();
        var request = new CreateStudyAreaWeekRequest(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 8));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectUnknownStudyArea()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var plan = StudyPlan.Create(planId, "Normal", 1m);
        var service = CreateService(planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectUnknownStudyPlan()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var service = CreateService(areaRepository: new FakeStudyAreaRepository(area));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectInactiveStudyPlan()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var plan = StudyPlan.Create(planId, "Inactive", 1m);
        plan.Deactivate();

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectDuplicateAreaInWeek()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var plan = StudyPlan.Create(planId, "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();

        var assessment = WeeklyAssessment.Create(2026, 37, 1500m);
        repository.AddWeeklyAssessment(assessment);
        repository.AddExisting(StudyAreaWeek.Create(TargetWeekStart, area, plan, assessment.Id, 1500m));

        var service = CreateService(
            repository: repository,
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectResultBelowMinimumGoal()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1000);
        var plan = StudyPlan.Create(planId, "Normal", 1m);

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldCreateStudyAreaWeekWithCalculatedGoal()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1200);
        var plan = StudyPlan.Create(planId, "Intensivo", 1.5m);
        var repository = new FakeStudyAreaWeekRepository();

        var service = CreateService(
            repository: repository,
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);
        var response = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(areaId, response.StudyAreaId);
        Assert.Equal(planId, response.StudyPlanId);
        Assert.Equal(TargetWeekStart, response.WeekStartDate);
        Assert.Equal(1800m, response.WeekIndividualGoal);
        Assert.Equal(1800m, response.WeekGlobalGoal);
        Assert.Equal(0, response.MinutesStudied);
        Assert.Single(repository.GetByWeek(TargetWeekStart));
    }

    [Fact]
    public async Task CreateShouldCalculateWeeklyAssessmentUsingIsoCalendar()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var plan = StudyPlan.Create(planId, "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();

        var service = CreateService(
            repository: repository,
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);
        var response = await service.CreateAsync(request, CancellationToken.None);

        var date = TargetWeekStart.ToDateTime(TimeOnly.MinValue);
        var year = ISOWeek.GetYear(date);
        var week = ISOWeek.GetWeekOfYear(date);
        var assessment = repository.GetWeeklyAssessment(year, week);

        Assert.NotNull(assessment);
        Assert.Equal(2026, assessment.Year);
        Assert.Equal(37, assessment.WeekNumber);
        Assert.Equal(response.WeeklyAssessmentId, assessment.Id);
        Assert.Equal(1500m, assessment.WeekGlobalGoal);
    }

    [Fact]
    public async Task CreateShouldReuseExistingWeeklyAssessment()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var existingAreaId = Guid.NewGuid();
        var existingPlanId = Guid.NewGuid();

        var area = StudyArea.Create(areaId, "C#", 1500);
        var plan = StudyPlan.Create(planId, "Normal", 1m);
        var existingArea = StudyArea.Create(existingAreaId, "SQL", 1500);
        var existingPlan = StudyPlan.Create(existingPlanId, "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();

        var assessment = WeeklyAssessment.Create(2026, 37, 1500m);
        repository.AddWeeklyAssessment(assessment);
        repository.AddExisting(StudyAreaWeek.Create(TargetWeekStart, existingArea, existingPlan, assessment.Id, 1500m));

        var service = CreateService(
            repository: repository,
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var response = await service.CreateAsync(
            new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart),
            CancellationToken.None);

        Assert.Equal(assessment.Id, response.WeeklyAssessmentId);
        Assert.Equal(3000m, response.WeekGlobalGoal);
        Assert.Equal(2, repository.GetByWeek(TargetWeekStart).Count);
    }

    [Fact]
    public async Task CreateShouldRejectCurrentWeek()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var plan = StudyPlan.Create(planId, "Normal", 1m);

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, CurrentWeekStart);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectWeekOutsideConfigurationWindow()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var plan = StudyPlan.Create(planId, "Normal", 1m);

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, CurrentWeekStart.AddDays(35));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldCreateAllItemsAtomically()
    {
        var area1 = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var area2 = StudyArea.Create(Guid.NewGuid(), "SQL", 500);
        var plan1 = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var plan2 = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 1m);
        var repository = new FakeStudyAreaWeekRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(area1, area2),
            new FakeStudyPlanRepository(plan1, plan2),
            unitOfWork);

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [
                new CreateStudyAreaWeekBatchItem(area1.Id, plan1.Id),
                new CreateStudyAreaWeekBatchItem(area2.Id, plan2.Id)
            ]);

        var response = await service.CreateBatchAsync(request, CancellationToken.None);

        Assert.Equal(TargetWeekStart, response.WeekStartDate);
        Assert.Equal(1500m, response.WeekGlobalGoal);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);

        var configurations = repository.GetByWeek(TargetWeekStart);
        Assert.Equal(2, configurations.Count);
        Assert.All(configurations, item => Assert.Equal(response.WeeklyAssessmentId, item.WeeklyAssessmentId));
        Assert.Equal(1000m, configurations.Single(x => x.StudyAreaId == area1.Id).Assessment.WeekIndividualGoal);
        Assert.Equal(500m, configurations.Single(x => x.StudyAreaId == area2.Id).Assessment.WeekIndividualGoal);
    }

    [Fact]
    public async Task CreateBatchShouldCalculateGlobalGoalFromAllItems()
    {
        var area1 = StudyArea.Create(Guid.NewGuid(), "C#", 1200);
        var area2 = StudyArea.Create(Guid.NewGuid(), "SQL", 1000);
        var plan1 = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var plan2 = StudyPlan.Create(Guid.NewGuid(), "Intensivo", 0.5m);
        var repository = new FakeStudyAreaWeekRepository();

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(area1, area2),
            new FakeStudyPlanRepository(plan1, plan2));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [
                new CreateStudyAreaWeekBatchItem(area1.Id, plan1.Id),
                new CreateStudyAreaWeekBatchItem(area2.Id, plan2.Id)
            ]);

        var response = await service.CreateBatchAsync(request, CancellationToken.None);

        Assert.Equal(1700m, response.WeekGlobalGoal);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1200m, response.Items.Single(x => x.StudyAreaId == area1.Id).WeekIndividualGoal);
        Assert.Equal(500m, response.Items.Single(x => x.StudyAreaId == area2.Id).WeekIndividualGoal);
    }

    [Fact]
    public async Task CreateBatchShouldUseSingleWeeklyAssessment()
    {
        var area1 = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var area2 = StudyArea.Create(Guid.NewGuid(), "SQL", 1000);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(area1, area2),
            new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [
                new CreateStudyAreaWeekBatchItem(area1.Id, plan.Id),
                new CreateStudyAreaWeekBatchItem(area2.Id, plan.Id)
            ]);

        var response = await service.CreateBatchAsync(request, CancellationToken.None);

        var configurations = repository.GetByWeek(TargetWeekStart);

        Assert.Equal(2, configurations.Count);
        Assert.All(
            configurations,
            configuration => Assert.Equal(response.WeeklyAssessmentId, configuration.WeeklyAssessmentId));

        var assessments = repository.GetAllWeeklyAssessments();

        var targetAssessment = Assert.Single(
            assessments,
            assessment => assessment.Year == 2026 && assessment.WeekNumber == 37);

        Assert.Equal(response.WeeklyAssessmentId, targetAssessment.Id);
        Assert.Equal(2000m, targetAssessment.WeekGlobalGoal);
    }

    [Fact]
    public async Task CreateBatchShouldRejectDuplicateArea()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(area),
            new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [
                new CreateStudyAreaWeekBatchItem(area.Id, plan.Id),
                new CreateStudyAreaWeekBatchItem(area.Id, plan.Id)
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));

        Assert.Empty(repository.GetByWeek(TargetWeekStart));
    }

    [Fact]
    public async Task CreateBatchShouldRejectExistingAreaInTargetWeek()
    {
        var existingArea = StudyArea.Create(Guid.NewGuid(), "Existing", 1500);
        var newArea = StudyArea.Create(Guid.NewGuid(), "New", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();
        var assessment = WeeklyAssessment.Create(2026, 37, 1500m);

        repository.AddWeeklyAssessment(assessment);
        repository.AddExisting(StudyAreaWeek.Create(TargetWeekStart, existingArea, plan, assessment.Id, 1500m));

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(newArea),
            new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [new CreateStudyAreaWeekBatchItem(existingArea.Id, plan.Id)]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectUnknownStudyArea()
    {
        var unknownAreaId = Guid.NewGuid();
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [new CreateStudyAreaWeekBatchItem(unknownAreaId, plan.Id)]);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectUnknownStudyPlan()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var unknownPlanId = Guid.NewGuid();

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository());

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [new CreateStudyAreaWeekBatchItem(area.Id, unknownPlanId)]);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectInactiveStudyPlan()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Inactive", 1m);
        plan.Deactivate();

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [new CreateStudyAreaWeekBatchItem(area.Id, plan.Id)]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectGlobalGoalBelowMinimum()
    {
        var area1 = StudyArea.Create(Guid.NewGuid(), "C#", 500);
        var area2 = StudyArea.Create(Guid.NewGuid(), "SQL", 500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);

        var repository = new FakeStudyAreaWeekRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(area1, area2),
            new FakeStudyPlanRepository(plan),
            unitOfWork);

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [
                new CreateStudyAreaWeekBatchItem(area1.Id, plan.Id),
                new CreateStudyAreaWeekBatchItem(area2.Id, plan.Id)
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));

        Assert.Empty(repository.GetByWeek(TargetWeekStart));
        Assert.Equal(0, unitOfWork.CommitCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task CreateBatchShouldRejectCurrentWeek()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan),
            unitOfWork: unitOfWork);

        var request = new CreateStudyAreaWeekBatchRequest(
            CurrentWeekStart,
            [new CreateStudyAreaWeekBatchItem(area.Id, plan.Id)]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task CreateBatchShouldRejectWeekOutsideConfigurationWindow()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan),
            unitOfWork: unitOfWork);

        var request = new CreateStudyAreaWeekBatchRequest(
            CurrentWeekStart.AddDays(35),
            [new CreateStudyAreaWeekBatchItem(area.Id, plan.Id)]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task CreateBatchShouldRollbackWhenSaveChangesFails()
    {
        var area1 = StudyArea.Create(Guid.NewGuid(), "C#", 1000);
        var area2 = StudyArea.Create(Guid.NewGuid(), "SQL", 500);
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1m);
        var repository = new FakeStudyAreaWeekRepository();
        var unitOfWork = new FakeUnitOfWork { ThrowOnSaveChanges = true };

        var service = CreateService(
            repository,
            new FakeStudyAreaRepository(area1, area2),
            new FakeStudyPlanRepository(plan),
            unitOfWork);

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [
                new CreateStudyAreaWeekBatchItem(area1.Id, plan.Id),
                new CreateStudyAreaWeekBatchItem(area2.Id, plan.Id)
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(2, repository.GetByWeek(TargetWeekStart).Count);
    }

    [Fact]
    public async Task CreateBatchShouldRejectNullItems()
    {
        var service = CreateService();

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            null!);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectEmptyItems()
    {
        var service = CreateService();

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            []);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectEmptyStudyAreaId()
    {
        var plan = StudyPlan.Create(Guid.NewGuid(), "Normal", 1500m);
        var service = CreateService(planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [new CreateStudyAreaWeekBatchItem(Guid.Empty, plan.Id)]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBatchShouldRejectEmptyStudyPlanId()
    {
        var area = StudyArea.Create(Guid.NewGuid(), "C#", 1500);
        var service = CreateService(areaRepository: new FakeStudyAreaRepository(area));

        var request = new CreateStudyAreaWeekBatchRequest(
            TargetWeekStart,
            [new CreateStudyAreaWeekBatchItem(area.Id, Guid.Empty)]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBatchAsync(request, CancellationToken.None));
    }

    private static StudyAreaWeekService CreateService(
        FakeStudyAreaWeekRepository? repository = null,
        FakeStudyAreaRepository? areaRepository = null,
        FakeStudyPlanRepository? planRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        var fakeRepository = repository ?? new FakeStudyAreaWeekRepository();
        fakeRepository.EnsureCurrentWeekIsAchieved(CurrentWeekStart);

        return new StudyAreaWeekService(
            fakeRepository,
            areaRepository ?? new FakeStudyAreaRepository(),
            planRepository ?? new FakeStudyPlanRepository(),
            new FakeApplicationCalendar(CurrentWeekStart),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private sealed class FakeStudyAreaRepository(params StudyArea[] areas) : IStudyAreaRepository
    {
        private readonly Dictionary<Guid, StudyArea> _areas = areas.ToDictionary(x => x.Id);

        public Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_areas.GetValueOrDefault(id));

        public Task<IReadOnlyList<StudyArea>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyArea>>(_areas.Values.ToList());

        public Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
            => Task.FromResult(_areas.Values.Any(x => x.Name == name && x.Id != excludedId));

        public Task<bool> HasDependenciesAsync(Guid studyAreaId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public void Add(StudyArea studyArea) => _areas[studyArea.Id] = studyArea;

        public void Remove(StudyArea studyArea) => _areas.Remove(studyArea.Id);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStudyPlanRepository(params StudyPlan[] plans) : IStudyPlanRepository
    {
        private readonly Dictionary<Guid, StudyPlan> _plans = plans.ToDictionary(x => x.Id);

        public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_plans.GetValueOrDefault(id));

        public Task<IReadOnlyList<StudyPlan>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyPlan>>(_plans.Values.ToList());

        public void Add(StudyPlan studyPlan) => _plans[studyPlan.Id] = studyPlan;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStudyAreaWeekRepository : IStudyAreaWeekRepository
    {
        private readonly List<StudyAreaWeek> _items = [];
        private readonly Dictionary<(int Year, int Week), WeeklyAssessment> _assessments = [];

        public void AddExisting(StudyAreaWeek item) => _items.Add(item);

        public void AddWeeklyAssessment(WeeklyAssessment assessment)
            => _assessments[(assessment.Year, assessment.WeekNumber)] = assessment;

        public void EnsureCurrentWeekIsAchieved(DateOnly weekStartDate)
        {
            var date = weekStartDate.ToDateTime(TimeOnly.MinValue);
            var year = ISOWeek.GetYear(date);
            var week = ISOWeek.GetWeekOfYear(date);

            if (_assessments.ContainsKey((year, week)))
                return;

            var assessment = WeeklyAssessment.Create(year, week, 1500m);
            _assessments[(year, week)] = assessment;

            var area = StudyArea.Create("Current Area", 1500);
            var plan = StudyPlan.Create("Current Plan", 1m);
            var configuration = StudyAreaWeek.Create(
                weekStartDate,
                area,
                plan,
                assessment.Id,
                1500m);

            configuration.Assessment.UpdateMinutesStudied(1500);
            _items.Add(configuration);
        }

        public List<StudyAreaWeek> GetByWeek(DateOnly weekStartDate)
            => _items.Where(x => x.WeekStartDate == weekStartDate).ToList();

        public List<WeeklyAssessment> GetAllWeeklyAssessments()
            => _assessments.Values.ToList();

        public WeeklyAssessment? GetWeeklyAssessment(int year, int weekNumber)
            => _assessments.GetValueOrDefault((year, weekNumber));

        public Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(
            DateOnly weekStartDate,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyAreaWeek>>(
                _items.Where(x => x.WeekStartDate == weekStartDate).ToList());

        public Task<bool> ExistsByAreaAndWeekAsync(
            Guid studyAreaId,
            DateOnly weekStartDate,
            CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(x =>
                x.StudyAreaId == studyAreaId &&
                x.WeekStartDate == weekStartDate));

        public Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(
            int year,
            int weekNumber,
            CancellationToken cancellationToken)
            => Task.FromResult(_assessments.GetValueOrDefault((year, weekNumber)));

        public void Add(StudyAreaWeek studyAreaWeek) => _items.Add(studyAreaWeek);

        public void AddWeeklyAssessmentForTest(WeeklyAssessment assessment)
            => _assessments[(assessment.Year, assessment.WeekNumber)] = assessment;
    }

    private sealed class FakeApplicationCalendar(DateOnly currentWeekStartDate) : IApplicationCalendar
    {
        public ApplicationWeek CurrentWeek => new(currentWeekStartDate);
        public ApplicationWeek PreviousWeek => CurrentWeek.AddWeeks(-1);
        public ApplicationWeek NextWeek => CurrentWeek.AddWeeks(1);

        public IReadOnlyList<ApplicationWeek> ConfigurationWeeks =>
        [
            CurrentWeek,
            NextWeek,
            NextWeek.AddWeeks(1),
            NextWeek.AddWeeks(2),
            NextWeek.AddWeeks(3)
        ];

        public ApplicationWeek GetWeek(DateOnly date) => new(date);

        public bool IsWithinConfigurationWindow(DateOnly weekStartDate)
            => weekStartDate >= CurrentWeek.WeekStartDate &&
               weekStartDate <= CurrentWeek.WeekStartDate.AddDays(28);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public bool ThrowOnSaveChanges { get; init; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnSaveChanges)
                throw new InvalidOperationException("Simulated persistence failure.");

            return Task.FromResult(1);
        }

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ITransaction>(new FakeTransaction(this));

        private sealed class FakeTransaction(FakeUnitOfWork owner) : ITransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                owner.CommitCount++;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                owner.RollbackCount++;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}