using StudyTime.Application;
using StudyTime.Infrastructure;
using StudyTime.Api.Middleware;
using StudyTime.Application.Abstractions;
using StudyTime.Infrastructure.Persistence.Transactions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ITransactionScope, EfTransactionScope>();

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