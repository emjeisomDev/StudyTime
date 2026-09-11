WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    Application = "StudyTime.Api",
    Version = "Foundation",
    Status = "Running"
}));

app.Run();

public partial class Program
{
}