using System.IO.Compression;
using StudyTime.Api.Filters;
using Microsoft.AspNetCore.ResponseCompression;

namespace StudyTime.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string AngularCorsPolicy = "AngularFrontend";

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        services.AddCors(options =>
        {
            options.AddPolicy(AngularCorsPolicy, policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        services.AddHealthChecks();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }
}
