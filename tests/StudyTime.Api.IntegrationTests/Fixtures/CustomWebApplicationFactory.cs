using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StudyTime.Api.IntegrationTests.Fixtures;

public sealed class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException(
                "A PostgreSQL connection string is required.",
                nameof(connectionString))
            : connectionString;

    private HttpClient? _client;

    public HttpClient Client => _client ??= CreateClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Cors:AllowedOrigins"] = "http://localhost:4200",
                    ["Database:ApplyMigrationsOnStartup"] = "false"
                });
        });
    }

    public void DisposeClient()
    {
        _client?.Dispose();
        _client = null;
    }
}
