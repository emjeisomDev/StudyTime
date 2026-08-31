using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration
    .GetConnectionString("StudyTime")
    ?? throw new InvalidOperationException(
        "Connection string 'StudyTime' was not configured."
    );

builder.Services.AddDbContext<StudyTimeDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(StudyTimeDbContext).Assembly.FullName)
    )
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new { service = "StudyTime.Api", status = "ok" }));

app.Run();

/// <summary>
/// Exposes the API program type for integration and foundation tests.
/// </summary>
public partial class Program;