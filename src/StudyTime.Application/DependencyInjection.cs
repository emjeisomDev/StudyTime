using FluentValidation;
using StudyTime.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using StudyTime.Application.WeeklyAssessments.Services;

namespace StudyTime.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IWeeklyAssessmentSynchronizer, WeeklyAssessmentSynchronizer>();

        return services;
    }
}