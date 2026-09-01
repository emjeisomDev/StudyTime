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
        var response = new StudyAreaWeekResponse(expectedId, request.StudyAreaId, request.StudyPlanId, request.WeekStartDate, Guid.NewGuid(), 1500m, 1500m, 0);
        var controller = new StudyAreaWeeksController(new FakeStudyAreaWeekService(response));

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(response, created.Value);
    }

    private sealed class FakeStudyAreaWeekService(StudyAreaWeekResponse response) : IStudyAreaWeekService
    {
        public Task<StudyAreaWeekResponse> CreateAsync(CreateStudyAreaWeekRequest request, CancellationToken cancellationToken) => Task.FromResult(response);
    }
}