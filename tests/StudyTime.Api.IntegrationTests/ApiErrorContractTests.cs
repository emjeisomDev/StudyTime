using Xunit;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using StudyTime.Api.IntegrationTests.Support;
using StudyTime.Api.IntegrationTests.Fixtures;

namespace StudyTime.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ApiErrorContractTests : ApiIntegrationTestBase
{
    public ApiErrorContractTests(ApiIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CriarStudyArea_ComMetaZero_Retorna400ValidationProblemDetails()
    {
        // Arrange
        object payload = new
        {
            name = "Área inválida",
            stdWeekStudyTime = 0
        };

        // Act
        using HttpResponseMessage response =
            await Client.PostAsJsonAsync("/study-areas", payload);

        // Assert
        using JsonDocument problem =
            await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);

        AssertValidationErrorsAreNotEmpty(problem);
    }

    [Fact]
    public async Task ConsultarStudyArea_Inexistente_Retorna404ProblemDetails()
    {
        // Arrange
        Guid unknownId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response =
            await Client.GetAsync($"/study-areas/{unknownId}");

        // Assert
        using JsonDocument problem =
            await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);

        Assert.Contains("not found", problem.RootElement.GetProperty("title").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BootstrapDuplicado_Retorna409ProblemDetails()
    {
        // Arrange
        StudyAreaApiResponse mathematics =
            await CreateStudyAreaAsync("Matemática", 600);
        StudyAreaApiResponse physics =
            await CreateStudyAreaAsync("Física", 900);
        StudyPlanApiResponse plan =
            await CreateStudyPlanAsync("Plano principal", 1m);

        using HttpResponseMessage firstResponse = await PostBootstrapAsync(
            (mathematics.Id, plan.Id),
            (physics.Id, plan.Id));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act
        using HttpResponseMessage duplicateResponse = await PostBootstrapAsync(
            (mathematics.Id, plan.Id),
            (physics.Id, plan.Id));

        // Assert: the API contract requires duplicate initialization to be a conflict.
        using JsonDocument problem =
            await AssertProblemDetailsAsync(
                duplicateResponse,
                HttpStatusCode.Conflict);

        Assert.Contains(
            "initialized",
            problem.RootElement.GetProperty("detail").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CriarStudyPlan_ComCoefficientZero_Retorna422ProblemDetails()
    {
        // Arrange
        object payload = new
        {
            name = "Plano inválido",
            coefficient = 0m
        };

        // Act
        using HttpResponseMessage response =
            await Client.PostAsJsonAsync("/study-plans", payload);

        // Assert: the contract requested for this scenario is 422.
        using JsonDocument problem =
            await AssertProblemDetailsAsync(
                response,
                HttpStatusCode.UnprocessableEntity);

        Assert.False(string.IsNullOrWhiteSpace(
            problem.RootElement.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Bootstrap_ComMetaGlobalDe1400Minutos_Retorna422ProblemDetails()
    {
        // Arrange
        StudyAreaApiResponse area =
            await CreateStudyAreaAsync("Área abaixo da meta", 1400);
        StudyPlanApiResponse plan =
            await CreateStudyPlanAsync("Plano principal", 1m);

        // Act
        using HttpResponseMessage response =
            await PostBootstrapAsync((area.Id, plan.Id));

        // Assert
        using JsonDocument problem =
            await AssertProblemDetailsAsync(
                response,
                HttpStatusCode.UnprocessableEntity);

        Assert.Contains(
            "1500",
            problem.RootElement.GetProperty("detail").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AtualizarStatusDoPlano_AceitaCaseInsensitiveERejeitaStatusDesconhecido()
    {
        // Arrange
        StudyPlanApiResponse plan =
            await CreateStudyPlanAsync("Plano para status", 1m);

        // Act: uppercase is accepted.
        using HttpResponseMessage inactiveResponse =
            await Client.PatchAsJsonAsync(
                $"/study-plans/{plan.Id}/status",
                new { status = "INACTIVE" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, inactiveResponse.StatusCode);

        StudyPlanApiResponse? updatedPlan =
            await inactiveResponse.Content.ReadFromJsonAsync<StudyPlanApiResponse>();

        Assert.NotNull(updatedPlan);
        Assert.Equal("Inactive", updatedPlan.Status, ignoreCase: true);

        // Act: unknown status must be rejected.
        using HttpResponseMessage invalidStatusResponse =
            await Client.PatchAsJsonAsync(
                $"/study-plans/{plan.Id}/status",
                new { status = "pendente" });

        // Assert
        using JsonDocument problem =
            await AssertProblemDetailsAsync(
                invalidStatusResponse,
                HttpStatusCode.UnprocessableEntity);

        Assert.Contains(
            "active",
            problem.RootElement.GetProperty("detail").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExcluirUltimoRegistroDeSemanaPassada_Retorna409RecordNotInCurrentWeek()
    {
        // Arrange: this past-week record is seeded directly because the API does
        // not permit creating a new configuration for a past week.
        Guid pastWeekId = await Fixture.SeedPreviousWeekAsync(includeStudyRecord: true);

        // Act
        using HttpResponseMessage response = await Client.DeleteAsync(
            $"/study-area-weeks/{pastWeekId}/study-records/last");

        // Assert
        using JsonDocument problem =
            await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict);

        Assert.Contains(
            "current week",
            problem.RootElement.GetProperty("detail").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }
}
