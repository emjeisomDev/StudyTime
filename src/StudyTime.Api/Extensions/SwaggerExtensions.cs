using System.Reflection;
using Microsoft.OpenApi;

namespace StudyTime.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StudyTime API",
                Version = "v1",
                Description = "API REST para gerenciamento de estudos."
            });

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var xmlFileName = $"{assemblyName}.xml";
            var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            if (File.Exists(xmlFilePath))
            {
                options.IncludeXmlComments(xmlFilePath);
            }
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    "/swagger/v1/swagger.json",
                    "StudyTime API v1");

                options.RoutePrefix = "swagger";
                options.DocumentTitle = "StudyTime API — Swagger";
            });
        }

        return app;
    }
}