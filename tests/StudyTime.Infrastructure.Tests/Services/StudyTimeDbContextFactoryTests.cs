using Xunit;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

namespace StudyTime.Infrastructure.Tests.Services;

[Collection("Environment variable isolation")]
public sealed class StudyTimeDbContextFactoryTests
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__DefaultConnection";
    private const string ExpectedFallbackConnectionString =
        "Host=localhost;Port=5433;Database=studytime;Username=studytime;Password=studytime_dev";

    [Fact]
    public void CreateDbContext_ConnectionStringAusente_UsaFallbackSemConectarAoBanco()
    {
        // Arrange
        string? originalValue = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, null);

            // Act
            using StudyTimeDbContext context = new StudyTimeDbContextFactory()
                .CreateDbContext(Array.Empty<string>());

            // Assert
            Assert.Equal(
                ExpectedFallbackConnectionString,
                context.Database.GetConnectionString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ConnectionStringEnvironmentVariable,
                originalValue);
        }
    }
}

[CollectionDefinition("Environment variable isolation", DisableParallelization = true)]
public sealed class EnvironmentVariableIsolationCollection
{
}
