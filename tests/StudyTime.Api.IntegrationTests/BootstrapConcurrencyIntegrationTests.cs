using Xunit;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Api.IntegrationTests.Support;
using StudyTime.Api.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace StudyTime.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class BootstrapConcurrencyIntegrationTests : ApiIntegrationTestBase
{
    public BootstrapConcurrencyIntegrationTests(ApiIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task BootstrapConcorrente_DuasRequisicoes_NaoCriaConfiguracoesDuplicadas()
    {
        // Arrange
        StudyAreaApiResponse mathematics = await CreateStudyAreaAsync("Matemática", 600);
        StudyAreaApiResponse physics = await CreateStudyAreaAsync("Física", 900);
        StudyPlanApiResponse plan = await CreateStudyPlanAsync("Plano principal", 1m);

        object Payload() => new
        {
            items = new[]
            {
                new { studyAreaId = mathematics.Id, studyPlanId = plan.Id },
                new { studyAreaId = physics.Id, studyPlanId = plan.Id }
            }
        };

        // Act
        Task<HttpResponseMessage> firstTask = Client.PostAsJsonAsync("/bootstrap", Payload());
        Task<HttpResponseMessage> secondTask = Client.PostAsJsonAsync("/bootstrap", Payload());

        HttpResponseMessage[] responses = await Task.WhenAll(firstTask, secondTask);

        try
        {
            // Assert: exactly one request can initialize the system.
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);

            HttpResponseMessage rejectedResponse = Assert.Single(
                    responses, 
                    response => response.StatusCode != HttpStatusCode.Created);

            using JsonDocument problem =
                await AssertProblemDetailsAsync(
                    rejectedResponse,
                    HttpStatusCode.Conflict);

            await using AsyncServiceScope scope =
                Fixture.Factory.Services.CreateAsyncScope();
            StudyTimeDbContext dbContext =
                scope.ServiceProvider.GetRequiredService<StudyTimeDbContext>();

            int currentWeekConfigurations = await dbContext.StudyAreaWeeks.CountAsync(
                item => item.WeekStartDate == ApiTestDateHelper.GetCurrentWeekStart());

            Assert.Equal(2, currentWeekConfigurations);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }
}