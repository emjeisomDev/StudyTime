using StudyTime.Api.Filters;
using StudyTime.Api.Middleware;
using StudyTime.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Registra o DbContext e demais serviços da Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddScoped<ProblemDetailsFactory>();
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AngularFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;