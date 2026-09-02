using Microsoft.AspNetCore.Mvc;
using StudyTime.Api.Controllers;
using StudyTime.Application.StudyAreaWeeks;

namespace Api.Tests.Controllers;

public sealed class StudyAreaWeeksControllerTests
{
    [Fact]
    public async Task CreateShouldReturnCreated()
    {
        var expectedId = Guid.NewGuid();
        var request = new CreateStudyAreaWeekRequest(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 7));
        var response = new StudyAreaWeekResponse(
            expectedId,
            request.StudyAreaId,
            request.StudyPlanId,
            request.WeekStartDate,
            Guid.NewGuid(),
            1500m,
            1500m,
            0);

        var service = new FakeStudyAreaWeekService(response, null);
        var controller = new StudyAreaWeeksController(service);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task CreateBatchShouldReturnCreated()
    {
        var area1 = Guid.NewGuid();
        var area2 = Guid.NewGuid();
        var plan1 = Guid.NewGuid();
        var plan2 = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();

        var request = new CreateStudyAreaWeekBatchRequest(
            new DateOnly(2026, 9, 7),
            [
                new CreateStudyAreaWeekBatchItem(area1, plan1),
                new CreateStudyAreaWeekBatchItem(area2, plan2)
            ]);

        var response = new StudyAreaWeekBatchResponse(
            request.WeekStartDate,
            assessmentId,
            2500m,
            [
                new StudyAreaWeekResponse(
                    Guid.NewGuid(),
                    area1,
                    plan1,
                    request.WeekStartDate,
                    assessmentId,
                    1500m,
                    2500m,
                    0),
                new StudyAreaWeekResponse(
                    Guid.NewGuid(),
                    area2,
                    plan2,
                    request.WeekStartDate,
                    assessmentId,
                    1000m,
                    2500m,
                    0)
            ]);

        var service = new FakeStudyAreaWeekService(null, response);
        var controller = new StudyAreaWeeksController(service);

        var result = await controller.CreateBatch(request, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task CreateBatchShouldForwardRequestToService()
    {
        var request = new CreateStudyAreaWeekBatchRequest(
            new DateOnly(2026, 9, 7),
            [new CreateStudyAreaWeekBatchItem(Guid.NewGuid(), Guid.NewGuid())]);

        var response = new StudyAreaWeekBatchResponse(
            request.WeekStartDate,
            Guid.NewGuid(),
            1500m,
            []);

        var service = new FakeStudyAreaWeekService(null, response);
        var controller = new StudyAreaWeeksController(service);

        await controller.CreateBatch(request, CancellationToken.None);

        Assert.Same(request, service.LastBatchRequest);
    }

    private sealed class FakeStudyAreaWeekService(
        StudyAreaWeekResponse? createResponse,
        StudyAreaWeekBatchResponse? batchResponse) : IStudyAreaWeekService
    {
        public CreateStudyAreaWeekBatchRequest? LastBatchRequest { get; private set; }

        public Task<StudyAreaWeekResponse> CreateAsync(
            CreateStudyAreaWeekRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(
                createResponse ?? throw new InvalidOperationException("Individual response was not configured."));

        public Task<StudyAreaWeekBatchResponse> CreateBatchAsync(
            CreateStudyAreaWeekBatchRequest request,
            CancellationToken cancellationToken)
        {
            LastBatchRequest = request;
            return Task.FromResult(
                batchResponse ?? throw new InvalidOperationException("Batch response was not configured."));
        }
    }
}