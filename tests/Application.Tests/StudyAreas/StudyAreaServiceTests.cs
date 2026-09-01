using StudyTime.Application.StudyAreas;
using StudyTime.Domain.Entities;

namespace Application.Tests.StudyAreas;

public sealed class StudyAreaServiceTests
{
    [Fact]
    public async Task CreateShouldPersistValidStudyArea()
    {
        var repository = new FakeStudyAreaRepository();
        var service = new StudyAreaService(repository);

        var result = await service.CreateAsync(new CreateStudyAreaRequest(" C# ", 600), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("C#", result.Name);
        Assert.Equal(600, result.StdWeekStudyTime);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task CreateShouldRejectDuplicateName()
    {
        var repository = new FakeStudyAreaRepository();
        repository.Add(StudyArea.Create("C#", 600));
        var service = new StudyAreaService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateStudyAreaRequest("C#", 800), CancellationToken.None));
    }

    [Fact]
    public async Task CreateShouldRejectInvalidStandardWeeklyStudyTime()
    {
        var repository = new FakeStudyAreaRepository();
        var service = new StudyAreaService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateAsync(new CreateStudyAreaRequest("C#", 0), CancellationToken.None));
    }

    [Fact]
    public async Task ListShouldReturnStudyAreasOrderedByName()
    {
        var repository = new FakeStudyAreaRepository();
        repository.Add(StudyArea.Create("SQL", 600));
        repository.Add(StudyArea.Create("C#", 600));
        var service = new StudyAreaService(repository);

        var result = await service.ListAsync(CancellationToken.None);

        Assert.Equal(["C#", "SQL"], result.Select(x => x.Name));
    }

    [Fact]
    public async Task GetByIdShouldReturnExistingStudyArea()
    {
        var repository = new FakeStudyAreaRepository();
        var area = StudyArea.Create("C#", 600);
        repository.Add(area);
        var service = new StudyAreaService(repository);

        var result = await service.GetByIdAsync(area.Id, CancellationToken.None);

        Assert.Equal(area.Id, result.Id);
        Assert.Equal("C#", result.Name);
        Assert.Equal(600, result.StdWeekStudyTime);
    }

    [Fact]
    public async Task GetByIdShouldRejectUnknownStudyArea()
    {
        var repository = new FakeStudyAreaRepository();
        var service = new StudyAreaService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateShouldChangeNameAndStandardWeeklyStudyTime()
    {
        var repository = new FakeStudyAreaRepository();
        var area = StudyArea.Create("C#", 600);
        repository.Add(area);
        var service = new StudyAreaService(repository);

        var result = await service.UpdateAsync(
            area.Id,
            new UpdateStudyAreaRequest("CSharp", 900),
            CancellationToken.None);

        Assert.Equal(area.Id, result.Id);
        Assert.Equal("CSharp", result.Name);
        Assert.Equal(900, result.StdWeekStudyTime);
        Assert.Equal("CSharp", area.Name);
        Assert.Equal(900, area.StdWeekStudyTime);
    }

    [Fact]
    public async Task UpdateShouldRejectDuplicateName()
    {
        var repository = new FakeStudyAreaRepository();
        var first = StudyArea.Create("C#", 600);
        var second = StudyArea.Create("SQL", 600);
        repository.Add(first);
        repository.Add(second);
        var service = new StudyAreaService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(
                second.Id,
                new UpdateStudyAreaRequest("C#", 900),
                CancellationToken.None));

        Assert.Equal("SQL", second.Name);
        Assert.Equal(600, second.StdWeekStudyTime);
    }

    [Fact]
    public async Task UpdateShouldNotModifyAnyWeeklyConfiguration()
    {
        var repository = new FakeStudyAreaRepository();
        var area = StudyArea.Create("C#", 600);
        repository.Add(area);
        repository.DependencyIds.Add(area.Id);
        var service = new StudyAreaService(repository);

        var result = await service.UpdateAsync(
            area.Id,
            new UpdateStudyAreaRequest("CSharp", 900),
            CancellationToken.None);

        Assert.Equal("CSharp", result.Name);
        Assert.Equal(900, result.StdWeekStudyTime);
        Assert.Contains(area.Id, repository.DependencyIds);
    }

    [Fact]
    public async Task DeleteShouldRemoveStudyAreaWithoutDependencies()
    {
        var repository = new FakeStudyAreaRepository();
        var area = StudyArea.Create("C#", 600);
        repository.Add(area);
        var service = new StudyAreaService(repository);

        await service.DeleteAsync(area.Id, CancellationToken.None);

        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task DeleteShouldRejectStudyAreaWithWeeklyDependencies()
    {
        var repository = new FakeStudyAreaRepository();
        var area = StudyArea.Create("C#", 600);
        repository.Add(area);
        repository.DependencyIds.Add(area.Id);
        var service = new StudyAreaService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(area.Id, CancellationToken.None));

        Assert.Single(repository.Items);
    }

    private sealed class FakeStudyAreaRepository : IStudyAreaRepository
    {
        public List<StudyArea> Items { get; } = [];
        public HashSet<Guid> DependencyIds { get; } = [];

        public Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<StudyArea>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyArea>>(Items.OrderBy(x => x.Name).ToArray());

        public Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
            => Task.FromResult(Items.Any(x => x.Name == name && (!excludedId.HasValue || x.Id != excludedId.Value)));

        public Task<bool> HasDependenciesAsync(Guid studyAreaId, CancellationToken cancellationToken)
            => Task.FromResult(DependencyIds.Contains(studyAreaId));

        public void Add(StudyArea studyArea)
            => Items.Add(studyArea);

        public void Remove(StudyArea studyArea)
            => Items.Remove(studyArea);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}