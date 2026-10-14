namespace StudyTime.Api.Tests.Fixtures;

public sealed class DatabaseResetFixture : IAsyncLifetime
{
    public ApiWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        await Factory.StartPostgresAsync();
        await Factory.EnsureDatabaseCreatedAsync();
    }

    public Task ResetAsync(
        CancellationToken cancellationToken = default)
    {
        return Factory.ResetDatabaseAsync(cancellationToken);
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiIntegrationTestCollection : ICollectionFixture<DatabaseResetFixture>
{
    public const string Name = "StudyTime API integration tests";
}
