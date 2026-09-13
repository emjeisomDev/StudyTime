using Xunit;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Infrastructure.Tests.Containers;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("studytime_tests")
        .WithUsername("studytime")
        .WithPassword("studytime")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}