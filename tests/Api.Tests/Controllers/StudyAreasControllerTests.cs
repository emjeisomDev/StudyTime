using Microsoft.AspNetCore.Mvc;
using StudyTime.Api.Controllers;
using StudyTime.Application.StudyAreas;
using StudyTime.Domain.Entities;

namespace Api.Tests.Controllers;

public sealed class StudyAreasControllerTests
{
    [Fact]
    public async Task CreateShouldReturnCreatedAtAction()
    {
        var service = new FakeStudyAreaService();
        var controller = new StudyAreasController(service);

        var result = await controller.Create(
            new CreateStudyAreaRequest("C#", 600),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<StudyAreaResponse>(created.Value);

        Assert.Equal(nameof(StudyAreasController.GetById), created.ActionName);
        Assert.Equal(response.Id, created.RouteValues!["id"]);
        Assert.Equal("C#", response.Name);
        Assert.Equal(600, response.StdWeekStudyTime);
    }

    [Fact]
    public async Task GetAllShouldReturnOk()
    {
        var service = new FakeStudyAreaService();
        service.Items.Add(new StudyAreaResponse(Guid.NewGuid(), "C#", 600));
        var controller = new StudyAreasController(service);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<StudyAreaResponse>>(ok.Value);

        Assert.Single(response);
        Assert.Equal("C#", response[0].Name);
    }

    [Fact]
    public async Task GetByIdShouldReturnOk()
    {
        var service = new FakeStudyAreaService();
        var id = Guid.NewGuid();
        service.Items.Add(new StudyAreaResponse(id, "C#", 600));
        var controller = new StudyAreasController(service);

        var result = await controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudyAreaResponse>(ok.Value);

        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task UpdateShouldReturnOk()
    {
        var service = new FakeStudyAreaService();
        var id = Guid.NewGuid();
        var controller = new StudyAreasController(service);

        var result = await controller.Update(
            id,
            new UpdateStudyAreaRequest("CSharp", 900),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudyAreaResponse>(ok.Value);

        Assert.Equal(id, response.Id);
        Assert.Equal("CSharp", response.Name);
        Assert.Equal(900, response.StdWeekStudyTime);
    }

    [Fact]
    public async Task DeleteShouldReturnNoContent()
    {
        var service = new FakeStudyAreaService();
        var controller = new StudyAreasController(service);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.True(service.DeleteCalled);
    }

    private sealed class FakeStudyAreaService : IStudyAreaService
    {
        public List<StudyAreaResponse> Items { get; } = [];
        public bool DeleteCalled { get; private set; }

        public Task<StudyAreaResponse> CreateAsync(
            CreateStudyAreaRequest request,
            CancellationToken cancellationToken)
        {
            var response = new StudyAreaResponse(
                Guid.NewGuid(),
                request.Name?.Trim() ?? string.Empty,
                request.StdWeekStudyTime);

            Items.Add(response);
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<StudyAreaResponse>> ListAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyAreaResponse>>(Items);

        public Task<StudyAreaResponse> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = Items.FirstOrDefault(x => x.Id == id)
                ?? new StudyAreaResponse(id, "C#", 600);

            return Task.FromResult(result);
        }

        public Task<StudyAreaResponse> UpdateAsync(
            Guid id,
            UpdateStudyAreaRequest request,
            CancellationToken cancellationToken)
        {
            var response = new StudyAreaResponse(
                id,
                request.Name?.Trim() ?? string.Empty,
                request.StdWeekStudyTime);

            return Task.FromResult(response);
        }

        public Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }
}