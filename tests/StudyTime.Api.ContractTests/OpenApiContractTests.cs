using Xunit;
using System.Net;
using System.Text.Json;
using Microsoft.OpenApi;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using StudyTime.Api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Controllers;
using StudyTime.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

namespace StudyTime.Api.ContractTests;

public sealed class OpenApiContractTests : IClassFixture<SwaggerContractFactory>, IDisposable
{
    private const string SwaggerDocumentPath = "/swagger/v1/swagger.json";
    private const string ValidationProblemDetailsSchemaRef =
        "#/components/schemas/ValidationProblemDetails";

    private static readonly JsonSerializerOptions WebJsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "patch", "delete", "head", "options", "trace"
    };

    private static readonly IReadOnlyDictionary<string, HashSet<string>> ExpectedOperations =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["/study-areas"] = ["GET", "POST"],
            ["/study-plans"] = ["GET", "POST"],
            ["/study-area-weeks"] = ["POST"],
            ["/study-area-weeks/batch"] = ["POST"],
            ["/study-area-weeks/{id}"] = ["GET", "PUT", "DELETE"],
            ["/bootstrap"] = ["POST"],
            ["/study-area-weeks/{studyAreaWeekId}/study-records"] = ["POST", "GET"],
            ["/study-area-weeks/{studyAreaWeekId}/study-records/last"] = ["DELETE"],
            ["/weeks/previous-summary"] = ["GET"],
            ["/study-areas/{id}"] = ["GET", "PUT"],
            ["/study-plans/{id}"] = ["GET"],
            ["/study-plans/{id}/status"] = ["PATCH"]
        };

    private static readonly IReadOnlyDictionary<string, Type> SchemaDtoTypes =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["ProblemDetails"] = typeof(ProblemDetails),
            ["ValidationProblemDetails"] = typeof(ValidationProblemDetails),
            ["BootstrapRequest"] = typeof(BootstrapRequest),
            ["CreateStudyAreaRequest"] = typeof(CreateStudyAreaRequest),
            ["StudyAreaResponse"] = typeof(StudyAreaResponse),
            ["CreateStudyPlanRequest"] = typeof(CreateStudyPlanRequest),
            ["StudyPlanResponse"] = typeof(StudyPlanResponse),
            ["StudyAreaWeekItemRequest"] = typeof(StudyAreaWeekItemRequest),
            ["CreateStudyAreaWeekRequest"] = typeof(CreateStudyAreaWeekRequest),
            ["CreateStudyAreaWeeksBatchRequest"] = typeof(CreateStudyAreaWeeksBatchRequest),
            ["UpdateStudyAreaWeekRequest"] = typeof(UpdateStudyAreaWeekRequest),
            ["StudyAreaWeekResponse"] = typeof(StudyAreaWeekResponse),
            ["CreateStudyRecordRequest"] = typeof(CreateStudyRecordRequest),
            ["StudyRecordResponse"] = typeof(StudyRecordResponse),
            ["WeeklyAssessmentResponse"] = typeof(WeeklyAssessmentResponse),
            ["PreviousWeekSummaryResponse"] = typeof(PreviousWeekSummaryResponse),
            ["UpdateStudyPlanStatusRequest"] = typeof(UpdateStudyPlanStatusRequest)
        };

    private readonly SwaggerContractFactory _factory;
    private readonly HttpClient _client;

    public OpenApiContractTests(SwaggerContractFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Contract_SwaggerRuntime_ContemOsDozeCaminhosEDezoitoOperacoes()
    {
        // Arrange
        using JsonDocument contract = LoadContract();
        using JsonDocument runtime = JsonDocument.Parse(await GetRuntimeSwaggerJsonAsync());

        // Act
        JsonElement contractPaths = contract.RootElement.GetProperty("paths");
        JsonElement runtimePaths = runtime.RootElement.GetProperty("paths");

        // Assert
        Assert.Equal(
            ExpectedOperations.Keys.OrderBy(path => path, StringComparer.Ordinal),
            contractPaths.EnumerateObject()
                .Select(path => path.Name)
                .OrderBy(path => path, StringComparer.Ordinal));

        Assert.Equal(
            ExpectedOperations.Keys.OrderBy(path => path, StringComparer.Ordinal),
            runtimePaths.EnumerateObject()
                .Select(path => path.Name)
                .OrderBy(path => path, StringComparer.Ordinal));

        int expectedOperationCount = 0;
        foreach ((string path, HashSet<string> expectedMethods) in ExpectedOperations)
        {
            HashSet<string> contractMethods = GetOperationMethods(contractPaths.GetProperty(path));
            HashSet<string> runtimeMethods = GetOperationMethods(runtimePaths.GetProperty(path));

            Assert.True(
                expectedMethods.SetEquals(contractMethods),
                $"The static contract has unexpected HTTP methods for '{path}'. Expected: " +
                $"{FormatMethods(expectedMethods)}; actual: {FormatMethods(contractMethods)}.");
            Assert.True(
                expectedMethods.SetEquals(runtimeMethods),
                $"Runtime Swagger has unexpected HTTP methods for '{path}'. Expected: " +
                $"{FormatMethods(expectedMethods)}; actual: {FormatMethods(runtimeMethods)}.");

            expectedOperationCount += expectedMethods.Count;
        }

        Assert.Equal(18, expectedOperationCount);
        Assert.Equal(18, CountOperations(contractPaths));
        Assert.Equal(18, CountOperations(runtimePaths));
    }

    [Fact]
    public async Task Contract_CadaOperacaoTemOperationIdERespostasDeclaradas()
    {
        // Arrange
        using JsonDocument contract = LoadContract();
        using JsonDocument runtime = JsonDocument.Parse(await GetRuntimeSwaggerJsonAsync());

        // Act + Assert
        AssertOperationsHaveIdsAndResponses(contract.RootElement.GetProperty("paths"), "StudyTime.v1.json");
        AssertOperationsHaveIdsAndResponses(runtime.RootElement.GetProperty("paths"), "runtime Swagger");
    }

    [Fact]
    public async Task Contract_TodasAsRespostas400ReferenciamValidationProblemDetails()
    {
        // Arrange
        using JsonDocument contract = LoadContract();
        using JsonDocument runtime = JsonDocument.Parse(await GetRuntimeSwaggerJsonAsync());
        JsonElement contractRoot = contract.RootElement;
        JsonElement contractPaths = contractRoot.GetProperty("paths");
        JsonElement runtimePaths = runtime.RootElement.GetProperty("paths");
        JsonElement reusableBadRequest = contractRoot
            .GetProperty("components")
            .GetProperty("responses")
            .GetProperty("BadRequest");

        // Act
        AssertResponseContentUsesSchemaRef(
            reusableBadRequest,
            ValidationProblemDetailsSchemaRef,
            "components.responses.BadRequest");

        int checkedResponses = 0;
        foreach ((string path, string method, JsonElement operation) in EnumerateOperations(contractPaths))
        {
            if (!operation.TryGetProperty("responses", out JsonElement responses) ||
                !responses.TryGetProperty("400", out JsonElement badRequest))
            {
                continue;
            }

            string location = $"{method.ToUpperInvariant()} {path} (contract 400)";
            Assert.True(
                badRequest.TryGetProperty("$ref", out JsonElement responseReference) &&
                string.Equals(
                    responseReference.GetString(),
                    "#/components/responses/BadRequest",
                    StringComparison.Ordinal),
                $"{location} must reference #/components/responses/BadRequest.");

            JsonElement runtimeOperation = runtimePaths
                .GetProperty(path)
                .GetProperty(method);
            Assert.True(
                runtimeOperation.TryGetProperty("responses", out JsonElement runtimeResponses),
                $"Runtime Swagger does not declare responses for {method.ToUpperInvariant()} {path}.");
            Assert.True(
                runtimeResponses.TryGetProperty("400", out JsonElement runtimeBadRequest),
                $"Runtime Swagger does not declare the 400 response for {method.ToUpperInvariant()} {path}.");

            AssertResponseContentUsesSchemaRef(
                runtimeBadRequest,
                ValidationProblemDetailsSchemaRef,
                $"runtime Swagger {method.ToUpperInvariant()} {path} response 400");
            checkedResponses++;
        }

        foreach ((string path, string method, JsonElement operation) in EnumerateOperations(runtimePaths))
        {
            if (operation.TryGetProperty("responses", out JsonElement responses) &&
                responses.TryGetProperty("400", out JsonElement badRequest))
            {
                AssertResponseContentUsesSchemaRef(
                    badRequest,
                    ValidationProblemDetailsSchemaRef,
                    $"runtime Swagger {method.ToUpperInvariant()} {path} response 400");
            }
        }

        // Assert
        Assert.True(checkedResponses > 0, "The contract should declare at least one 400 response.");
    }

    [Fact]
    public void Contract_DtosDeRespostaSerializados_RespeitamSchemasRequiredTiposCamelCaseESemantica()
    {
        // Arrange
        using JsonDocument contract = LoadContract();
        JsonElement root = contract.RootElement;
        JsonElement schemas = root.GetProperty("components").GetProperty("schemas");
        Guid areaId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        Guid planId = Guid.Parse("22222222-2222-4222-8222-222222222222");
        Guid weekId = Guid.Parse("44444444-4444-4444-8444-444444444444");
        Guid weeklyAssessmentId = Guid.Parse("55555555-5555-4555-8555-555555555555");

        WeeklyAssessmentResponse weeklyAssessment = new(
            weeklyAssessmentId,
            43,
            2026,
            1500m,
            1420,
            false);

        var responseSamples = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["StudyAreaWeekResponse"] = new StudyAreaWeekResponse(
                weekId,
                new DateOnly(2026, 10, 19),
                areaId,
                planId,
                weeklyAssessmentId,
                1500m,
                1420,
                false),
            ["WeeklyAssessmentResponse"] = weeklyAssessment,
            ["PreviousWeekSummaryResponse"] = new PreviousWeekSummaryResponse(
                new DateOnly(2026, 10, 19),
                weeklyAssessment,
                new[]
                {
                    new PreviousWeekSummaryItemResponse(
                        new StudyAreaResponse(areaId, "Circuitos Elétricos", 600),
                        new StudyPlanResponse(planId, "Plano de revisão", 2.5m, "active"),
                        1500m,
                        1420,
                        false)
                })
        };

        // Act + Assert
        foreach ((string schemaName, object dto) in responseSamples)
        {
            JsonElement serialized = JsonSerializer.SerializeToElement(
                dto,
                dto.GetType(),
                WebJsonOptions);
            JsonElement schema = schemas.GetProperty(schemaName);

            AssertMatchesSchema(
                serialized,
                schema,
                schemas,
                $"serialized DTO {schemaName}");
            AssertJsonPropertyNamesAreCamelCase(serialized, $"serialized DTO {schemaName}");
        }

        JsonElement weeklyGoalAchieved = schemas
            .GetProperty("WeeklyAssessmentResponse")
            .GetProperty("properties")
            .GetProperty("goalAchieved");
        Assert.True(
            weeklyGoalAchieved.TryGetProperty("readOnly", out JsonElement readOnly) &&
            readOnly.ValueKind == JsonValueKind.True,
            "WeeklyAssessmentResponse.goalAchieved must be readOnly in the contract.");

        string globalGoalDescription = weeklyGoalAchieved.GetProperty("description").GetString() ?? string.Empty;
        Assert.Contains("atingimento simultâneo de todas as metas individuais", globalGoalDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Não compara diretamente", globalGoalDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GoalAchievedEvaluator.EvaluateGlobal", globalGoalDescription, StringComparison.Ordinal);

        JsonElement individualGoalAchieved = schemas
            .GetProperty("StudyAreaWeekResponse")
            .GetProperty("properties")
            .GetProperty("goalAchieved");
        Assert.True(
            individualGoalAchieved.TryGetProperty("readOnly", out JsonElement individualReadOnly) &&
            individualReadOnly.ValueKind == JsonValueKind.True,
            "StudyAreaWeekResponse.goalAchieved must be readOnly in the contract.");
    }

    [Fact]
    public void Contract_TodosOsExemplosDeclarados_SaoConformesEDesserializamNosDtosDaApi()
    {
        // Arrange
        using JsonDocument contract = LoadContract();
        JsonElement root = contract.RootElement;
        JsonElement components = root.GetProperty("components");
        JsonElement schemas = components.GetProperty("schemas");
        int exampleCount = 0;

        // Act + Assert: examples attached to schema definitions.
        foreach (JsonProperty schemaProperty in schemas.EnumerateObject())
        {
            if (schemaProperty.Value.TryGetProperty("example", out JsonElement example))
            {
                JsonElement schemaReference = CreateSchemaReference(schemaProperty.Name);
                ValidateExample(
                    example,
                    schemaReference,
                    schemas,
                    $"components.schemas.{schemaProperty.Name}.example");
                exampleCount++;
            }
        }

        // Reusable response examples include success/error payloads and ValidationProblemDetails.
        if (components.TryGetProperty("responses", out JsonElement reusableResponses))
        {
            foreach (JsonProperty responseProperty in reusableResponses.EnumerateObject())
            {
                if (responseProperty.Value.TryGetProperty("content", out JsonElement content))
                {
                    foreach (JsonProperty mediaProperty in content.EnumerateObject())
                    {
                        exampleCount += ValidateMediaExamples(
                            mediaProperty.Value,
                            schemas,
                            $"components.responses.{responseProperty.Name}.content.{mediaProperty.Name}");
                    }
                }
            }
        }

        // Request and inline response examples for each documented operation.
        foreach (JsonProperty pathProperty in root.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operationProperty in pathProperty.Value.EnumerateObject())
            {
                if (!HttpMethods.Contains(operationProperty.Name))
                {
                    continue;
                }

                JsonElement operation = operationProperty.Value;
                string operationLocation =
                    $"paths.{pathProperty.Name}.{operationProperty.Name}";

                if (operation.TryGetProperty("requestBody", out JsonElement requestBody) &&
                    requestBody.TryGetProperty("content", out JsonElement requestContent))
                {
                    foreach (JsonProperty mediaProperty in requestContent.EnumerateObject())
                    {
                        exampleCount += ValidateMediaExamples(
                            mediaProperty.Value,
                            schemas,
                            $"{operationLocation}.requestBody.content.{mediaProperty.Name}");
                    }
                }

                if (operation.TryGetProperty("responses", out JsonElement responses))
                {
                    foreach (JsonProperty responseProperty in responses.EnumerateObject())
                    {
                        if (!responseProperty.Value.TryGetProperty("content", out JsonElement responseContent))
                        {
                            // Reusable responses are validated at components.responses above.
                            continue;
                        }

                        foreach (JsonProperty mediaProperty in responseContent.EnumerateObject())
                        {
                            exampleCount += ValidateMediaExamples(
                                mediaProperty.Value,
                                schemas,
                                $"{operationLocation}.responses.{responseProperty.Name}.content.{mediaProperty.Name}");
                        }
                    }
                }
            }
        }

        // Assert
        Assert.True(exampleCount >= 20, $"Expected to validate the contract examples; validated {exampleCount}.");

        int ValidateMediaExamples(JsonElement media, JsonElement schemaDefinitions, string location)
        {
            if (!media.TryGetProperty("schema", out JsonElement schema))
            {
                Assert.False(
                    media.TryGetProperty("example", out _) || media.TryGetProperty("examples", out _),
                    $"{location} declares an example without a schema.");
                return 0;
            }

            int mediaExampleCount = 0;
            if (media.TryGetProperty("example", out JsonElement singleExample))
            {
                ValidateExample(singleExample, schema, schemaDefinitions, $"{location}.example");
                mediaExampleCount++;
            }

            if (media.TryGetProperty("examples", out JsonElement namedExamples))
            {
                foreach (JsonProperty exampleProperty in namedExamples.EnumerateObject())
                {
                    if (exampleProperty.Value.TryGetProperty("value", out JsonElement value))
                    {
                        ValidateExample(
                            value,
                            schema,
                            schemaDefinitions,
                            $"{location}.examples.{exampleProperty.Name}.value");
                        mediaExampleCount++;
                    }
                }
            }

            return mediaExampleCount;
        }
    }

    [Fact]
    public async Task Contract_RotasDosControllersViaEndpointDataSource_EstaoTodasDocumentadasNoSwagger()
    {
        // Arrange
        using JsonDocument runtime = JsonDocument.Parse(await GetRuntimeSwaggerJsonAsync());
        HashSet<string> documentedOperations = EnumerateOperations(runtime.RootElement.GetProperty("paths"))
            .Select(operation => FormatOperation(operation.Path, operation.Method))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IEnumerable<Endpoint> endpoints = _factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints;
        var controllerOperations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Act
        foreach (RouteEndpoint endpoint in endpoints.OfType<RouteEndpoint>())
        {
            if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is null)
            {
                continue;
            }

            string rawRoute = endpoint.RoutePattern.RawText
                ?? endpoint.RoutePattern.ToString()
                ?? throw new Xunit.Sdk.XunitException(
                    "Controller endpoint has no route pattern text.");
            string path = NormalizeRoute(rawRoute);
            HttpMethodMetadata methodMetadata =
                endpoint.Metadata.GetMetadata<HttpMethodMetadata>() ??
                throw new Xunit.Sdk.XunitException(
                    $"Controller endpoint '{path}' has no HTTP method metadata.");

            foreach (string method in methodMetadata.HttpMethods)
            {
                controllerOperations.Add(FormatOperation(path, method));
            }
        }

        // Assert
        string[] undocumented = controllerOperations
            .Except(documentedOperations, StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] staleDocumentation = documentedOperations
            .Except(controllerOperations, StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            undocumented.Length == 0 && staleDocumentation.Length == 0,
            "Controller/Swagger route mismatch. Undocumented controller operations: " +
            $"[{string.Join(", ", undocumented)}]. Swagger operations without controller endpoints: " +
            $"[{string.Join(", ", staleDocumentation)}].");
    }

    public void Dispose() => _client.Dispose();

    private async Task<string> GetRuntimeSwaggerJsonAsync()
    {
        using HttpResponseMessage response = await _client.GetAsync(SwaggerDocumentPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(json), "Runtime Swagger returned an empty document.");
        AssertOpenApiReaderCanLoad(json, "runtime Swagger");
        return json;
    }

    private static JsonDocument LoadContract()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Contracts", "StudyTime.v1.json");
        Assert.True(File.Exists(path), $"Static OpenAPI contract file was not copied to test output: '{path}'.");

        string json = File.ReadAllText(path);
        AssertOpenApiReaderCanLoad(json, "StudyTime.v1.json");
        return JsonDocument.Parse(json);
    }

    private static void AssertOpenApiReaderCanLoad(string json, string source)
    {
        var result = OpenApiDocument.Parse(json);

        Assert.NotNull(result.Document);

        var diagnostic = result.Diagnostic;
        Assert.NotNull(diagnostic);
        Assert.True(
            !diagnostic.Errors.Any(),
            $"Microsoft.OpenApi could not parse '{source}'. Errors: " +
            string.Join("; ", diagnostic.Errors));
    }

    private static HashSet<string> GetOperationMethods(JsonElement pathItem) =>
        pathItem.EnumerateObject()
            .Where(property => HttpMethods.Contains(property.Name))
            .Select(property => property.Name.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static int CountOperations(JsonElement paths) =>
        paths.EnumerateObject()
            .Sum(path => path.Value.EnumerateObject().Count(property => HttpMethods.Contains(property.Name)));

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> EnumerateOperations(JsonElement paths)
    {
        foreach (JsonProperty path in paths.EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (HttpMethods.Contains(operation.Name))
                {
                    yield return (path.Name, operation.Name.ToLowerInvariant(), operation.Value);
                }
            }
        }
    }

    private static void AssertOperationsHaveIdsAndResponses(JsonElement paths, string source)
    {
        int count = 0;
        foreach ((string path, string method, JsonElement operation) in EnumerateOperations(paths))
        {
            string location = $"{source}: {method.ToUpperInvariant()} {path}";

            Assert.True(
                operation.TryGetProperty("operationId", out JsonElement operationId) &&
                operationId.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(operationId.GetString()),
                $"{location} must declare a non-empty operationId.");

            Assert.True(
                operation.TryGetProperty("responses", out JsonElement responses) &&
                responses.ValueKind == JsonValueKind.Object &&
                responses.EnumerateObject().Any(),
                $"{location} must declare at least one response.");
            count++;
        }

        Assert.Equal(18, count);
    }

    private static void AssertResponseContentUsesSchemaRef(
        JsonElement response,
        string expectedSchemaRef,
        string location)
    {
        Assert.True(
            response.TryGetProperty("content", out JsonElement content) &&
            content.ValueKind == JsonValueKind.Object,
            $"{location} must declare response content.");

        bool usesExpectedSchema = content.EnumerateObject().Any(mediaProperty =>
            mediaProperty.Value.TryGetProperty("schema", out JsonElement schema) &&
            schema.TryGetProperty("$ref", out JsonElement schemaReference) &&
            string.Equals(schemaReference.GetString(), expectedSchemaRef, StringComparison.Ordinal));

        Assert.True(
            usesExpectedSchema,
            $"{location} must reference '{expectedSchemaRef}' in its response content schema.");
    }

    private static JsonElement CreateSchemaReference(string schemaName) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["$ref"] = $"#/components/schemas/{schemaName}"
        });

    private static void ValidateExample(
        JsonElement example,
        JsonElement schema,
        JsonElement schemaDefinitions,
        string location)
    {
        AssertMatchesSchema(example, schema, schemaDefinitions, location);

        Type dtoType = ResolveDtoType(schema);
        object? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize(example.GetRawText(), dtoType, WebJsonOptions);
        }
        catch (JsonException exception)
        {
            throw new Xunit.Sdk.XunitException(
                $"Contract example '{location}' could not be deserialized as '{dtoType.FullName}': {exception.Message}");
        }

        Assert.NotNull(deserialized);
    }

    private static Type ResolveDtoType(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out JsonElement reference))
        {
            string? referenceValue = reference.GetString();
            string schemaName = referenceValue?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                ?? throw new Xunit.Sdk.XunitException("The schema contains an invalid $ref.");

            Assert.True(
                SchemaDtoTypes.TryGetValue(schemaName, out Type? dtoType),
                $"No API DTO mapping exists for OpenAPI schema '{schemaName}'.");
            return dtoType!;
        }

        if (schema.TryGetProperty("type", out JsonElement type) &&
            string.Equals(type.GetString(), "array", StringComparison.Ordinal))
        {
            Assert.True(schema.TryGetProperty("items", out JsonElement items), "An array schema must declare items.");
            return ResolveDtoType(items).MakeArrayType();
        }

        throw new Xunit.Sdk.XunitException(
            $"Cannot map inline OpenAPI schema to an API DTO: {schema.GetRawText()}");
    }

    private static void AssertMatchesSchema(
        JsonElement value,
        JsonElement schema,
        JsonElement schemaDefinitions,
        string location)
    {
        if (schema.TryGetProperty("$ref", out JsonElement reference))
        {
            string referenceValue = reference.GetString() ?? string.Empty;
            const string prefix = "#/components/schemas/";
            Assert.True(
                referenceValue.StartsWith(prefix, StringComparison.Ordinal),
                $"Unsupported schema reference '{referenceValue}' at {location}.");
            string schemaName = referenceValue[prefix.Length..];
            Assert.True(
                schemaDefinitions.TryGetProperty(schemaName, out JsonElement referencedSchema),
                $"Schema reference '{referenceValue}' does not resolve at {location}.");
            AssertMatchesSchema(value, referencedSchema, schemaDefinitions, location);
            return;
        }

        if (!schema.TryGetProperty("type", out JsonElement typeProperty))
        {
            return;
        }

        string? schemaType = typeProperty.GetString();
        switch (schemaType)
        {
            case "object":
                Assert.Equal(JsonValueKind.Object, value.ValueKind);
                ValidateObject(value, schema, schemaDefinitions, location);
                break;

            case "array":
                Assert.Equal(JsonValueKind.Array, value.ValueKind);
                Assert.True(schema.TryGetProperty("items", out JsonElement items), $"Array schema has no items at {location}.");
                int index = 0;
                foreach (JsonElement item in value.EnumerateArray())
                {
                    AssertMatchesSchema(item, items, schemaDefinitions, $"{location}[{index}]");
                    index++;
                }
                break;

            case "string":
                Assert.Equal(JsonValueKind.String, value.ValueKind);
                ValidateStringFormat(value.GetString()!, schema, location);
                break;

            case "integer":
                Assert.Equal(JsonValueKind.Number, value.ValueKind);
                Assert.True(value.TryGetInt64(out long integerValue), $"Expected an integer at {location}.");
                if (schema.TryGetProperty("format", out JsonElement integerFormat) &&
                    string.Equals(integerFormat.GetString(), "int32", StringComparison.Ordinal))
                {
                    Assert.InRange(integerValue, (long)int.MinValue, (long)int.MaxValue);
                }
                break;

            case "number":
                Assert.Equal(JsonValueKind.Number, value.ValueKind);
                Assert.True(value.TryGetDecimal(out _), $"Expected a finite JSON number at {location}.");
                break;

            case "boolean":
                Assert.True(
                    value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                    $"Expected a boolean at {location}.");
                break;

            case "null":
                Assert.Equal(JsonValueKind.Null, value.ValueKind);
                break;

            default:
                throw new Xunit.Sdk.XunitException(
                    $"Unsupported OpenAPI schema type '{schemaType}' at {location}.");
        }
    }

    private static void ValidateObject(
        JsonElement value,
        JsonElement schema,
        JsonElement schemaDefinitions,
        string location)
    {
        JsonElement properties = default;
        bool hasProperties = schema.TryGetProperty("properties", out properties);

        if (schema.TryGetProperty("required", out JsonElement required))
        {
            foreach (JsonElement requiredProperty in required.EnumerateArray())
            {
                string name = requiredProperty.GetString()!;
                Assert.True(
                    value.TryGetProperty(name, out _),
                    $"Required property '{name}' is absent at {location}.");
                Assert.Equal(
                    JsonNamingPolicy.CamelCase.ConvertName(name),
                    name);
            }
        }

        foreach (JsonProperty jsonProperty in value.EnumerateObject())
        {
            Assert.Equal(
                JsonNamingPolicy.CamelCase.ConvertName(jsonProperty.Name),
                jsonProperty.Name);

            if (hasProperties && properties.TryGetProperty(jsonProperty.Name, out JsonElement propertySchema))
            {
                AssertMatchesSchema(
                    jsonProperty.Value,
                    propertySchema,
                    schemaDefinitions,
                    $"{location}.{jsonProperty.Name}");
                continue;
            }

            if (schema.TryGetProperty("additionalProperties", out JsonElement additionalProperties))
            {
                if (additionalProperties.ValueKind == JsonValueKind.False)
                {
                    Assert.Fail($"Unexpected property '{jsonProperty.Name}' at {location}.");
                }

                if (additionalProperties.ValueKind == JsonValueKind.Object)
                {
                    AssertMatchesSchema(
                        jsonProperty.Value,
                        additionalProperties,
                        schemaDefinitions,
                        $"{location}.{jsonProperty.Name}");
                }
            }
        }
    }

    private static void ValidateStringFormat(string value, JsonElement schema, string location)
    {
        if (!schema.TryGetProperty("format", out JsonElement formatProperty))
        {
            return;
        }

        string? format = formatProperty.GetString();
        switch (format)
        {
            case "uuid":
                Assert.True(Guid.TryParse(value, out _), $"Expected a UUID string at {location}.");
                break;

            case "date":
                Assert.True(
                    DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                    $"Expected an ISO date string at {location}.");
                break;

            case "date-time":
                Assert.True(
                    DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                    $"Expected an ISO date-time string at {location}.");
                break;
        }
    }

    private static void AssertJsonPropertyNamesAreCamelCase(JsonElement value, string location)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                Assert.Equal(
                    JsonNamingPolicy.CamelCase.ConvertName(property.Name),
                    property.Name);
                AssertJsonPropertyNamesAreCamelCase(property.Value, $"{location}.{property.Name}");
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                AssertJsonPropertyNamesAreCamelCase(item, $"{location}[{index}]");
                index++;
            }
        }
    }

    private static string NormalizeRoute(string route)
    {
        string trimmed = route.Trim('/');
        string withoutConstraints = Regex.Replace(
            trimmed,
            @"\{([^{}:]+):[^{}]+\}",
            "{$1}",
            RegexOptions.CultureInvariant);
        return $"/{withoutConstraints}";
    }

    private static string FormatOperation(string path, string method) =>
        $"{method.ToUpperInvariant()} {path}";

    private static string FormatMethods(IEnumerable<string> methods) =>
        string.Join(", ", methods.OrderBy(method => method, StringComparer.OrdinalIgnoreCase));
}

public sealed class SwaggerContractFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=studytime_contract_tests;Username=unused;Password=unused",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            ServiceDescriptor[] backgroundServiceRegistrations = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType == typeof(WeeklyRolloverBackgroundService))
                .ToArray();

            foreach (ServiceDescriptor descriptor in backgroundServiceRegistrations)
            {
                services.Remove(descriptor);
            }
        });
    }
}
