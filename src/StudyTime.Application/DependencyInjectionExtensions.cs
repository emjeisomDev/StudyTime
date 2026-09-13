using Microsoft.Extensions.DependencyInjection;

namespace StudyTime.Application;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services;
    
}