using Xunit;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Domain.Entities;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using StudyTime.Domain.ValueObjects;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Api.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace StudyTime.Api.IntegrationTests.Fixtures;

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("studytime_api_integration_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private readonly Dictionary<string, string?> _originalEnvironment = new();

    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public HttpClient Client => Factory.Client;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _postgres.GetConnectionString());
        SetEnvironmentVariable("Cors__AllowedOrigins", "http://localhost:4200");
        SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", "false");

        await using (StudyTimeDbContext dbContext = CreateDbContext())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        Factory = new CustomWebApplicationFactory(_postgres.GetConnectionString());

        // Start the host only after the Testcontainers database schema exists.
        _ = Factory.Client;
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            Factory.DisposeClient();
            Factory.Dispose();
        }

        await _postgres.DisposeAsync();

        foreach (KeyValuePair<string, string?> variable in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        StudyTimeDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<StudyTimeDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE " +
            "tb_study_record, " +
            "tb_study_area_week_assessment, " +
            "tb_study_area_week, " +
            "tb_study_area, " +
            "tb_study_plan, " +
            "tb_weekly_assessment " +
            "RESTART IDENTITY CASCADE;");
    }

    public async Task<Guid> SeedPreviousWeekAsync(bool includeStudyRecord)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        StudyTimeDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<StudyTimeDbContext>();

        DateOnly previousWeekStart = ApiTestDateHelper.GetCurrentWeekStart().AddDays(-7);
        DateTime previousWeekDateTime = previousWeekStart.ToDateTime(TimeOnly.MinValue);
        int isoYear = System.Globalization.ISOWeek.GetYear(previousWeekDateTime);
        int isoWeekNumber = System.Globalization.ISOWeek.GetWeekOfYear(previousWeekDateTime);

        StudyArea area = new(
            "Área da semana anterior",
            new Minutes(600));

        StudyPlan plan = new(
            "Plano da semana anterior",
            new Coefficient(1m));

        WeeklyAssessment weeklyAssessment = new(
            isoWeekNumber,
            isoYear,
            1500m,
            includeStudyRecord ? 60 : 600);

        await dbContext.StudyAreas.AddAsync(area);
        await dbContext.StudyPlans.AddAsync(plan);
        await dbContext.WeeklyAssessments.AddAsync(weeklyAssessment);
        await dbContext.SaveChangesAsync();

        StudyAreaWeek studyAreaWeek = new(
            previousWeekStart,
            area.Id,
            plan.Id,
            weeklyAssessment.Id);

        StudyAreaWeekAssessment individualAssessment = new(
            600m,
            studyAreaWeek.Id,
            includeStudyRecord ? 60 : 600);

        await dbContext.StudyAreaWeeks.AddAsync(studyAreaWeek);
        await dbContext.StudyAreaWeekAssessments.AddAsync(individualAssessment);

        if (includeStudyRecord)
        {
            await dbContext.StudyRecords.AddAsync(
                new StudyRecord(
                    previousWeekStart.AddDays(1),
                    new Minutes(60),
                    studyAreaWeek.Id));
        }

        await dbContext.SaveChangesAsync();

        return studyAreaWeek.Id;
    }

    private StudyTimeDbContext CreateDbContext()
    {
        DbContextOptions<StudyTimeDbContext> options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        return new StudyTimeDbContext(options);
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        if (!_originalEnvironment.ContainsKey(name))
        {
            _originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
        }

        Environment.SetEnvironmentVariable(name, value);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiIntegrationFixture>
{
    public const string Name = "StudyTime API integration tests";
}

public abstract class ApiIntegrationTestBase : IAsyncLifetime
{
    protected ApiIntegrationTestBase(ApiIntegrationFixture fixture)
    {
        Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    protected ApiIntegrationFixture Fixture { get; }

    protected HttpClient Client => Fixture.Client;

    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<StudyAreaApiResponse> CreateStudyAreaAsync(
        string name,
        int stdWeekStudyTime)
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/study-areas",
            new { name, stdWeekStudyTime });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        StudyAreaApiResponse? result =
            await response.Content.ReadFromJsonAsync<StudyAreaApiResponse>();

        return result ?? throw new InvalidOperationException(
            "The API returned an empty StudyArea response.");
    }

    protected async Task<StudyPlanApiResponse> CreateStudyPlanAsync(
        string name,
        decimal coefficient)
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/study-plans",
            new { name, coefficient });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        StudyPlanApiResponse? result =
            await response.Content.ReadFromJsonAsync<StudyPlanApiResponse>();

        return result ?? throw new InvalidOperationException(
            "The API returned an empty StudyPlan response.");
    }

    protected Task<HttpResponseMessage> PostBootstrapAsync(
        params (Guid StudyAreaId, Guid StudyPlanId)[] items)
    {
        object payload = new
        {
            items = items.Select(item => new
            {
                studyAreaId = item.StudyAreaId,
                studyPlanId = item.StudyPlanId
            }).ToArray()
        };

        return Client.PostAsJsonAsync("/bootstrap", payload);
    }

    protected async Task<JsonDocument> AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        string json = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.Equal(
            response.RequestMessage?.RequestUri?.AbsolutePath,
            root.GetProperty("instance").GetString());
        Assert.Equal((int)expectedStatus, root.GetProperty("status").GetInt32());

        return document;
    }

    protected static void AssertValidationErrorsAreNotEmpty(JsonDocument problemDocument)
    {
        JsonElement errors = problemDocument.RootElement.GetProperty("errors");

        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.Contains(
            errors.EnumerateObject(),
            property => property.Value.ValueKind == JsonValueKind.Array &&
                property.Value.EnumerateArray().Any(
                    message => !string.IsNullOrWhiteSpace(message.GetString())));
    }
}

public static class ApiTestDateHelper
{
    public static DateOnly GetCurrentWeekStart()
    {
        TimeZoneInfo officialTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            officialTimeZone);

        int daysSinceMonday =
            ((int)localNow.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

        return DateOnly.FromDateTime(localNow.Date.AddDays(-daysSinceMonday));
    }
}