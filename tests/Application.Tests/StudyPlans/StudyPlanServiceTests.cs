using StudyTime.Application.StudyPlans;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;

namespace Application.Tests.StudyPlans;

public sealed class StudyPlanServiceTests
{
    [Fact]
    public async Task CreateShouldPersistActiveStudyPlan()
    {
        var repository = new FakeStudyPlanRepository();
        var service = new StudyPlanService(repository);

        var result = await service.CreateAsync(
            new CreateStudyPlanRequest("Intensivo", 1.25m),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Intensivo", result.Name);
        Assert.Equal(1.25m, result.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, result.Status);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task CreateShouldTrimName()
    {
        var repository = new FakeStudyPlanRepository();
        var service = new StudyPlanService(repository);

        var result = await service.CreateAsync(
            new CreateStudyPlanRequest(" Intensivo ", 1.25m),
            CancellationToken.None);

        Assert.Equal("Intensivo", result.Name);
    }

    [Fact]
    public async Task CreateShouldRejectMissingName()
    {
        var repository = new FakeStudyPlanRepository();
        var service = new StudyPlanService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(
                new CreateStudyPlanRequest(" ", 1.25m),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectNonPositiveCoefficient()
    {
        var repository = new FakeStudyPlanRepository();
        var service = new StudyPlanService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateAsync(
                new CreateStudyPlanRequest("Intensivo", 0m),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectNameLongerThanEightyCharacters()
    {
        var repository = new FakeStudyPlanRepository();
        var service = new StudyPlanService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(
                new CreateStudyPlanRequest(new string('A', 81), 1m),
                CancellationToken.None));
    }

    [Fact]
    public async Task ListShouldReturnStudyPlansOrderedByName()
    {
        var repository = new FakeStudyPlanRepository();
        repository.Add(StudyPlan.Create("Z", 1m));
        repository.Add(StudyPlan.Create("A", 1m));
        var service = new StudyPlanService(repository);

        var result = await service.ListAsync(CancellationToken.None);

        Assert.Equal(["A", "Z"], result.Select(x => x.Name));
    }

    [Fact]
    public async Task GetByIdShouldReturnExistingStudyPlan()
    {
        var repository = new FakeStudyPlanRepository();
        var plan = StudyPlan.Create("Intensivo", 1.25m);
        repository.Add(plan);
        var service = new StudyPlanService(repository);

        var result = await service.GetByIdAsync(plan.Id, CancellationToken.None);

        Assert.Equal(plan.Id, result.Id);
        Assert.Equal("Intensivo", result.Name);
        Assert.Equal(1.25m, result.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetByIdShouldRejectUnknownStudyPlan()
    {
        var repository = new FakeStudyPlanRepository();
        var service = new StudyPlanService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateShouldChangeNameAndCoefficientWithoutChangingStatus()
    {
        var repository = new FakeStudyPlanRepository();
        var plan = StudyPlan.Create("Intensivo", 1.25m);
        repository.Add(plan);
        var service = new StudyPlanService(repository);

        var result = await service.UpdateAsync(
            plan.Id,
            new UpdateStudyPlanRequest("Leve", 0.75m),
            CancellationToken.None);

        Assert.Equal(plan.Id, result.Id);
        Assert.Equal("Leve", result.Name);
        Assert.Equal(0.75m, result.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, result.Status);
    }

    [Fact]
    public async Task UpdateShouldRejectInvalidCoefficientWithoutPartialMutation()
    {
        var repository = new FakeStudyPlanRepository();
        var plan = StudyPlan.Create("Intensivo", 1.25m);
        repository.Add(plan);
        var service = new StudyPlanService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAsync(
                plan.Id,
                new UpdateStudyPlanRequest("Leve", 0m),
                CancellationToken.None));

        Assert.Equal("Intensivo", plan.Name);
        Assert.Equal(1.25m, plan.Coefficient);
    }

    [Fact]
    public async Task ChangeStatusShouldDeactivateStudyPlan()
    {
        var repository = new FakeStudyPlanRepository();
        var plan = StudyPlan.Create("Intensivo", 1.25m);
        repository.Add(plan);
        var service = new StudyPlanService(repository);

        var result = await service.ChangeStatusAsync(
            plan.Id,
            new ChangeStudyPlanStatusRequest(StudyPlanStatus.Inactive),
            CancellationToken.None);

        Assert.Equal(StudyPlanStatus.Inactive, result.Status);
        Assert.Equal(StudyPlanStatus.Inactive, plan.Status);
    }

    [Fact]
    public async Task ChangeStatusShouldReactivateStudyPlan()
    {
        var repository = new FakeStudyPlanRepository();
        var plan = StudyPlan.Create("Intensivo", 1.25m, StudyPlanStatus.Inactive);
        repository.Add(plan);
        var service = new StudyPlanService(repository);

        var result = await service.ChangeStatusAsync(
            plan.Id,
            new ChangeStudyPlanStatusRequest(StudyPlanStatus.Active),
            CancellationToken.None);

        Assert.Equal(StudyPlanStatus.Active, result.Status);
        Assert.Equal(StudyPlanStatus.Active, plan.Status);
    }

    [Fact]
    public async Task ChangeStatusShouldRejectInvalidStatus()
    {
        var repository = new FakeStudyPlanRepository();
        var plan = StudyPlan.Create("Intensivo", 1.25m);
        repository.Add(plan);
        var service = new StudyPlanService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ChangeStatusAsync(
                plan.Id,
                new ChangeStudyPlanStatusRequest((StudyPlanStatus)99),
                CancellationToken.None));

        Assert.Equal(StudyPlanStatus.Active, plan.Status);
    }

    private sealed class FakeStudyPlanRepository : IStudyPlanRepository
    {
        public List<StudyPlan> Items { get; } = [];

        public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<StudyPlan>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyPlan>>(
                Items.OrderBy(x => x.Name).ThenBy(x => x.Id).ToArray());

        public void Add(StudyPlan studyPlan)
            => Items.Add(studyPlan);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}