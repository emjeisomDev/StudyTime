var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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