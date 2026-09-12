using Microsoft.EntityFrameworkCore;
using StudyTime.Application.Persistence;
using Microsoft.Extensions.Configuration;
using StudyTime.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace StudyTime.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration
            .GetConnectionString("StudyTime")
            ?? throw new InvalidOperationException("The 'StudyTime' connection string has not been configured.");

        services.AddDbContext<StudyTimeDbContext>(
            options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(StudyTimeDbContext).Assembly.FullName);
                    });

                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging(false);
            });

        services.AddScoped<IStudyTimeDbContext>
            (serviceProvider => serviceProvider.GetRequiredService<StudyTimeDbContext>());

        return services;
    }
}