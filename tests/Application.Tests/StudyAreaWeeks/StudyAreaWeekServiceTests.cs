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
        var areaRepository = new FakeStudyAreaRepository();
        var plan = StudyPlan.Create(planId, "Normal", 1m);
        var service = CreateService(areaRepository: areaRepository, planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectUnknownStudyPlan()
    {
        var areaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var area = StudyArea.Create(areaId, "C#", 1500);
        var service = CreateService(
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository());

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

        repository.AddExisting(
            StudyAreaWeek.Create(
                TargetWeekStart,
                area,
                plan,
                Guid.NewGuid(),
                1500m));

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

        var targetWeek = repository.GetByWeek(TargetWeekStart);

        Assert.Single(targetWeek);
        Assert.Equal(response.Id, targetWeek[0].Id);
        Assert.Equal(response.WeeklyAssessmentId, targetWeek[0].WeeklyAssessmentId);
        Assert.Equal(1800m, targetWeek[0].Assessment.WeekIndividualGoal);
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
        var weekNumber = ISOWeek.GetWeekOfYear(date);

        var assessment = repository.GetWeeklyAssessment(year, weekNumber);

        Assert.NotNull(assessment);
        Assert.Equal(2026, assessment.Year);
        Assert.Equal(37, assessment.WeekNumber);
        Assert.Equal(response.WeeklyAssessmentId, assessment.Id);
        Assert.Equal(1500m, assessment.WeekGlobalGoal);
    }

    [Fact]
    public async Task CreateShouldReuseWeeklyAssessmentForExistingTargetWeek()
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

        var existingAssessment = WeeklyAssessment.Create(2026, 37, 1500m);
        repository.AddWeeklyAssessment(existingAssessment);
        repository.AddExisting(
            StudyAreaWeek.Create(
                TargetWeekStart,
                existingArea,
                existingPlan,
                existingAssessment.Id,
                1500m));

        var service = CreateService(
            repository: repository,
            areaRepository: new FakeStudyAreaRepository(area),
            planRepository: new FakeStudyPlanRepository(plan));

        var request = new CreateStudyAreaWeekRequest(areaId, planId, TargetWeekStart);

        var response = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(existingAssessment.Id, response.WeeklyAssessmentId);
        Assert.Equal(3000m, response.WeekGlobalGoal);
        Assert.Equal(2, repository.GetByWeek(TargetWeekStart).Count);

        var assessments = repository.GetAllWeeklyAssessments();
        var targetAssessments = assessments.Where(x => x.Year == 2026 && x.WeekNumber == 37).ToList();

        Assert.Single(targetAssessments);
        Assert.Equal(existingAssessment.Id, targetAssessments[0].Id);
        Assert.Equal(3000m, targetAssessments[0].WeekGlobalGoal);
    }

    [Fact]
    public async Task CreateShouldRejectCurrentWeekWhenManualCreationIsRequested()
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

    private static StudyAreaWeekService CreateService(
        FakeStudyAreaWeekRepository? repository = null,
        FakeStudyAreaRepository? areaRepository = null,
        FakeStudyPlanRepository? planRepository = null)
    {
        var fakeRepository = repository ?? new FakeStudyAreaWeekRepository();
        fakeRepository.EnsureCurrentWeekIsAchieved(CurrentWeekStart);

        return new StudyAreaWeekService(
            fakeRepository,
            areaRepository ?? new FakeStudyAreaRepository(),
            planRepository ?? new FakeStudyPlanRepository(),
            new FakeApplicationCalendar(CurrentWeekStart),
            new FakeUnitOfWork());
    }

    private sealed class FakeStudyAreaRepository(StudyArea? result = null) : IStudyAreaRepository
    {
        private StudyArea? _result = result;

        public void SetResult(StudyArea? result) => _result = result;

        public Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_result);

        public Task<IReadOnlyList<StudyArea>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyArea>>(_result is null ? [] : [_result]);

        public Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<bool> HasDependenciesAsync(Guid studyAreaId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public void Add(StudyArea studyArea) { }

        public void Remove(StudyArea studyArea) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeStudyPlanRepository(StudyPlan? result = null) : IStudyPlanRepository
    {
        private readonly StudyPlan? _result = result;

        public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_result);

        public Task<IReadOnlyList<StudyPlan>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyPlan>>(_result is null ? [] : [_result]);

        public void Add(StudyPlan studyPlan) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeStudyAreaWeekRepository : IStudyAreaWeekRepository
    {
        private readonly List<StudyAreaWeek> _items = [];
        private readonly Dictionary<(int Year, int Week), WeeklyAssessment> _assessments = [];

        public void AddExisting(StudyAreaWeek item)
            => _items.Add(item);

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
            var currentConfiguration = StudyAreaWeek.Create(
                weekStartDate,
                area,
                plan,
                assessment.Id,
                1500m);

            currentConfiguration.Assessment.UpdateMinutesStudied(1500);
            _items.Add(currentConfiguration);
        }

        public List<StudyAreaWeek> GetByWeek(DateOnly weekStartDate)
            => _items.Where(x => x.WeekStartDate == weekStartDate).ToList();

        public List<WeeklyAssessment> GetAllWeeklyAssessments()
            => _assessments.Values.ToList();

        public WeeklyAssessment? GetWeeklyAssessment(int year, int weekNumber)
            => _assessments.TryGetValue((year, weekNumber), out var assessment)
                ? assessment
                : null;

        public Task<IReadOnlyList<StudyAreaWeek>> ListByWeekAsync(
            DateOnly weekStartDate,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyAreaWeek>>(
                _items.Where(x => x.WeekStartDate == weekStartDate).ToList());

        public Task<bool> ExistsByAreaAndWeekAsync(
            Guid studyAreaId,
            DateOnly weekStartDate,
            CancellationToken cancellationToken)
            => Task.FromResult(
                _items.Any(x =>
                    x.StudyAreaId == studyAreaId &&
                    x.WeekStartDate == weekStartDate));

        public Task<WeeklyAssessment?> GetWeeklyAssessmentAsync(
            int year,
            int weekNumber,
            CancellationToken cancellationToken)
            => Task.FromResult(
                _assessments.TryGetValue((year, weekNumber), out var assessment)
                    ? assessment
                    : null);

        public void Add(StudyAreaWeek studyAreaWeek)
            => _items.Add(studyAreaWeek);

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

        public ApplicationWeek GetWeek(DateOnly dateWeek)
            => new(dateWeek);

        public bool IsWithinConfigurationWindow(DateOnly weekStartDate)
            => weekStartDate >= CurrentWeek.WeekStartDate &&
               weekStartDate <= CurrentWeek.WeekStartDate.AddDays(28);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ITransaction>(new FakeTransaction());
    }

    private sealed class FakeTransaction : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}