using System.Reflection;
using Microsoft.OpenApi;

namespace StudyTime.Api.Extensions;

public static class SwaggerExtensions
{
    private static readonly IReadOnlyDictionary<(string Controller, string Action), string> OperationIds =
        new Dictionary<(string Controller, string Action), string>
        {
            [("Bootstrap", "Create")] = "BootstrapSystem",
            [("StudyAreas", "GetAll")] = "GetStudyAreas",
            [("StudyAreas", "Create")] = "CreateStudyArea",
            [("StudyAreas", "GetById")] = "GetStudyAreaById",
            [("StudyAreas", "Update")] = "UpdateStudyArea",
            [("StudyPlans", "GetAll")] = "GetStudyPlans",
            [("StudyPlans", "Create")] = "CreateStudyPlan",
            [("StudyPlans", "GetById")] = "GetStudyPlanById",
            [("StudyPlans", "UpdateStatus")] = "UpdateStudyPlanStatus",
            [("StudyAreaWeeks", "Create")] = "CreateStudyAreaWeek",
            [("StudyAreaWeeks", "CreateBatch")] = "CreateStudyAreaWeeksBatch",
            [("StudyAreaWeeks", "GetById")] = "GetStudyAreaWeekById",
            [("StudyAreaWeeks", "Update")] = "UpdateStudyAreaWeek",
            [("StudyAreaWeeks", "Delete")] = "DeleteStudyAreaWeek",
            [("StudyRecords", "Create")] = "CreateStudyRecord",
            [("StudyRecords", "GetByWeek")] = "GetStudyRecordsByWeek",
            [("StudyRecords", "DeleteLast")] = "DeleteLastStudyRecord",
            [("Weeks", "GetPreviousSummary")] = "GetPreviousWeekSummary"
        };

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StudyTime API",
                Version = "v1",
                Description = "API REST para gerenciamento de estudos."
            });

            // Keep runtime Swagger operationIds aligned with the checked-in contract.
            options.CustomOperationIds(apiDescription =>
            {
                string? controllerName = apiDescription.ActionDescriptor.RouteValues
                    .TryGetValue("controller", out string? controllerValue)
                    ? controllerValue
                    : null;
                string? actionName = apiDescription.ActionDescriptor.RouteValues
                    .TryGetValue("action", out string? actionValue)
                    ? actionValue
                    : null;

                if (controllerName is not null && actionName is not null &&
                    OperationIds.TryGetValue((controllerName, actionName), out string? operationId))
                {
                    return operationId;
                }

                // Preserve a non-empty identifier for future MVC actions not yet in the map.
                string method = apiDescription.HttpMethod ?? "Operation";
                string route = apiDescription.RelativePath ?? "Root";
                string normalizedRoute = route
                    .Replace('/', '_')
                    .Replace('{', '_')
                    .Replace('}', '_')
                    .Replace(':', '_')
                    .Replace('-', '_');
                return $"{method}_{normalizedRoute}";
            });

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "StudyTime.Api";
            string xmlFileName = $"{assemblyName}.xml";
            string xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            if (File.Exists(xmlFilePath))
            {
                options.IncludeXmlComments(xmlFilePath);
            }
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "StudyTime API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "StudyTime API — Swagger";
            });
        }

        return app;
    }
}
