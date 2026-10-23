using StudyTime.Application;
using StudyTime.Api.Filters;
using StudyTime.Api.Mappings;
using StudyTime.Api.Extensions;
using StudyTime.Api.Middleware;
using StudyTime.Infrastructure;
using StudyTime.Application.Mappings;

var builder = WebApplication.CreateBuilder(args);

var isProduction = builder.Environment.IsProduction();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

const string connectionStringMissingMessage =
    "A connection string não foi configurada. Defina a variável de ambiente ConnectionStrings__DefaultConnection.";

var connectionStringMissing = string.IsNullOrWhiteSpace(connectionString);

if (connectionStringMissing && isProduction)
{
    throw new InvalidOperationException(connectionStringMissingMessage);
}

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

var rawOrigins = configuredOrigins is { Length: > 0 }
    ? configuredOrigins
    : builder.Configuration["Cors:AllowedOrigins"]?
        .Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries);

var origins = rawOrigins?
    .SelectMany(origin => origin.Split(
        new[] { ';', ',' },
        StringSplitOptions.TrimEntries))
    .Select(origin => origin.Trim())
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? Array.Empty<string>();

if (origins.Length == 0)
{
    if (isProduction)
    {
        throw new InvalidOperationException(
            "Nenhuma origem CORS foi configurada para Production. " +
            "Defina Cors__AllowedOrigins com pelo menos uma origem HTTPS " +
            "ou HTTP válida, separada por ';' ou ','.");
    }

    origins = new[] { "http://localhost:4200" };
}

var normalizedOrigins = origins
    .Select(origin =>
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                $"Origem CORS inválida: '{origin}'. " +
                "Informe somente a origem, por exemplo " +
                "'https://frontend.example.com' ou 'http://localhost:4200'");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    })
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddApplication();

builder.Services.AddAutoMapper(
    cfg => { },
    typeof(ApiMappingProfile).Assembly,
    typeof(MappingProfile).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);

// Registra os mapeamentos DTO -> Response da camada API.
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(ApiMappingProfile).Assembly);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddOpenApi();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy
            .WithOrigins(normalizedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddScoped<ProblemDetailsFactory>();

var app = builder.Build();

if (connectionStringMissing)
{
    app.Logger.LogWarning(
        "{Message} A aplicação está fora de Production.",
        connectionStringMissingMessage);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AngularFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;