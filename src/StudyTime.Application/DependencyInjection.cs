using Microsoft.Extensions.DependencyInjection;

namespace StudyTime.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        // Assinatura correta para AutoMapper 13+
        services.AddAutoMapper(cfg => { }, assembly);

        return services;
    }
}