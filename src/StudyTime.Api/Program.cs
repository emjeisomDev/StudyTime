using StudyTime.Api.Middleware;
using StudyTime.Application;
using StudyTime.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseMiddleware<ProblemDetailsExceptionMiddleware>();

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