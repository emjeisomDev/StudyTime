using Microsoft.AspNetCore.Http;
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
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
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
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
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

    [Fact]
    public async Task GetAssessmentShouldReturnOk()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var response = new StudyAreaWeekAssessmentResponse(
            studyAreaWeekId,
            1500m,
            1500,
            true);

        var service = new FakeStudyAreaWeekService(null, null, response);
        var controller = new StudyAreaWeeksController(service);

        var result = await controller.GetAssessment(studyAreaWeekId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Equal(response, ok.Value);
        Assert.Equal(studyAreaWeekId, service.LastAssessmentId);
    }

    [Fact]
    public async Task GetAssessmentShouldReturnNotFoundWhenAssessmentDoesNotExist()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var service = new FakeStudyAreaWeekService(null, null, null);
        var controller = new StudyAreaWeeksController(service);

        var result = await controller.GetAssessment(studyAreaWeekId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, ((NotFoundResult)result.Result!).StatusCode);
        Assert.Equal(studyAreaWeekId, service.LastAssessmentId);
    }

    private sealed class FakeStudyAreaWeekService(
        StudyAreaWeekResponse? createResponse,
        StudyAreaWeekBatchResponse? batchResponse,
        StudyAreaWeekAssessmentResponse? assessmentResponse = null) : IStudyAreaWeekService
    {
        public CreateStudyAreaWeekBatchRequest? LastBatchRequest { get; private set; }
        public Guid? LastAssessmentId { get; private set; }

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

        public Task<StudyAreaWeekAssessmentResponse?> GetAssessmentAsync(
            Guid studyAreaWeekId,
            CancellationToken cancellationToken)
        {
            LastAssessmentId = studyAreaWeekId;
            return Task.FromResult(assessmentResponse);
        }
    }
}