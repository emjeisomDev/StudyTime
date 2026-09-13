using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyTime.Application.Abstractions;
using StudyTime.Application.Persistence;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Persistence.Transactions;
using StudyTime.Infrastructure.Time;

namespace StudyTime.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("StudyTime")
            ?? throw new InvalidOperationException(
                "The 'StudyTime' connection string has not been configured.");

        services.AddDbContext<StudyTimeDbContext>(
            options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(
                            typeof(StudyTimeDbContext).Assembly.FullName);
                    });

                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging(false);
            });

        services.AddScoped<IStudyTimeDbContext>(
            serviceProvider =>
                serviceProvider.GetRequiredService<StudyTimeDbContext>());

        services.AddSingleton<IApplicationClock, SystemApplicationClock>();
        services.AddSingleton<IWeekCalendar, SystemWeekCalendar>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<EfUnitOfWork>(
            serviceProvider =>
                (EfUnitOfWork)serviceProvider.GetRequiredService<IUnitOfWork>());

        services.AddScoped<ITransactionScope>(
            serviceProvider =>
                serviceProvider
                    .GetRequiredService<EfUnitOfWork>()
                    .GetCurrentTransactionScope());

        return services;
    }
}