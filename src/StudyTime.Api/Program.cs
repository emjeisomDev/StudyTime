using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StudyTime.Api.Errors;
using StudyTime.Application.Common.Clock;
using StudyTime.Application.Common.Transactions;
using StudyTime.Application.StudyAreas;
using StudyTime.Application.StudyPlans;
using StudyTime.Infrastructure.Common.Clock;
using StudyTime.Infrastructure.Common.Transactions;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var connectionString = builder.Configuration.GetConnectionString("StudyTime")
    ?? throw new InvalidOperationException("Connection string 'StudyTime' was not configured.");

var applicationTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IApplicationClock>(serviceProvider =>
    new ApplicationClock(
        serviceProvider.GetRequiredService<TimeProvider>(),
        applicationTimeZone));

builder.Services.AddDbContext<StudyTimeDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(StudyTimeDbContext).Assembly.FullName)));

builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IStudyAreaRepository, StudyAreaRepository>();
builder.Services.AddScoped<IStudyAreaService, StudyAreaService>();
builder.Services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
builder.Services.AddScoped<IStudyPlanService, StudyPlanService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "StudyTime.Api", status = "ok" }));

app.Run();


/// <summary>
/// Exposes the API program type for integration and foundation tests.
/// </summary>
public partial class Program;