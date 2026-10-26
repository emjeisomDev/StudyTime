using System.IO.Compression;
using StudyTime.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;

namespace StudyTime.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string AngularCorsPolicy = "AngularFrontend";

    /// <summary>
    /// Registers HTTP/API services using the CORS origins validated by Program.cs.
    /// This is the single registration point for controllers, CORS, health checks,
    /// and response compression.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IReadOnlyCollection<string> normalizedOrigins)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(normalizedOrigins);

        if (normalizedOrigins.Count == 0)
        {
            throw new ArgumentException(
                "At least one validated CORS origin is required.",
                nameof(normalizedOrigins));
        }

        services.AddScoped<ValidationFilter>();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });
        services.AddControllers(options =>
        {
            options.Filters.AddService<ValidationFilter>();
        });

        services.AddCors(options =>
        {
            options.AddPolicy(AngularCorsPolicy, policy =>
            {
                policy
                    .WithOrigins(normalizedOrigins.ToArray())
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        services.AddHealthChecks();

        services.AddResponseCompression(options =>
        {
            // Keep HTTPS compression disabled by default to avoid exposing dynamic
            // secrets to compression side-channel risks (e.g., BREACH).
            options.EnableForHttps = false;
            // ProblemDetails is served as application/problem+json, not application/json.
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                .Concat(new[] { "application/problem+json" });
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