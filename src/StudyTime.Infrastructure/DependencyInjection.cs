using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudyTime.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
//using StudyTime.Application.StudyAreaWeeks.AutoCreation;
// using StudyTime.Domain.Abstractions;
// using StudyTime.Domain.Abstractions.Repositories;
// using StudyTime.Infrastructure.Persistence.Repositories;
// using StudyTime.Infrastructure.Time;
// using StudyTime.Infrastructure.Calendar;

namespace StudyTime.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString =
            configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:DefaultConnection' was not configured.");
        }

        services.AddDbContext<StudyTimeDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // services.AddScoped<IStudyAreaRepository, StudyAreaRepository>();
        // services.AddScoped<IStudyAreaWeekAssessmentRepository, StudyAreaWeekAssessmentRepository>();
        // services.AddScoped<IStudyAreaWeekRepository, StudyAreaWeekRepository>();
        // services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
        // services.AddScoped<IStudyRecordRepository, StudyRecordRepository>();
        // services.AddScoped<IWeeklyAssessmentRepository, WeeklyAssessmentRepository>();

        // services.AddScoped<IUnitOfWork, UnitOfWork>();
        // services.AddSingleton<IClock, SystemClock>();
        // services.AddSingleton<ICurrentWeekProvider, CurrentWeekProvider>();
        // services.AddSingleton<IIsoWeekCalendar, IsoWeekCalendar>();
        // services.AddScoped<IStudyRecordLifoSelector, StudyRecordLifoSelector>();

        // services.AddScoped<IAutoCreateNextWeekService, AutoCreateNextWeekService>();

        return services;
    }
}