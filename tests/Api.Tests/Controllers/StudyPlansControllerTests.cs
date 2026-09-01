using Microsoft.AspNetCore.Mvc;
using StudyTime.Api.Controllers;
using StudyTime.Application.StudyPlans;
using StudyTime.Domain.Enums;

namespace Api.Tests.Controllers;

public sealed class StudyPlansControllerTests
{
    [Fact]
    public async Task CreateShouldReturnCreatedAtAction()
    {
        var service = new FakeStudyPlanService();
        var controller = new StudyPlansController(service);

        var result = await controller.Create(
            new CreateStudyPlanRequest("Intensivo", 1.25m),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<StudyPlanResponse>(created.Value);

        Assert.Equal(nameof(StudyPlansController.GetById), created.ActionName);
        Assert.Equal(response.Id, created.RouteValues!["id"]);
        Assert.Equal("Intensivo", response.Name);
        Assert.Equal(1.25m, response.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, response.Status);
    }

    [Fact]
    public async Task GetAllShouldReturnOk()
    {
        var service = new FakeStudyPlanService();
        service.Items.Add(new StudyPlanResponse(Guid.NewGuid(), "Intensivo", 1.25m, StudyPlanStatus.Active));
        var controller = new StudyPlansController(service);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<StudyPlanResponse>>(ok.Value);

        Assert.Single(response);
        Assert.Equal("Intensivo", response[0].Name);
    }

    [Fact]
    public async Task GetByIdShouldReturnOk()
    {
        var service = new FakeStudyPlanService();
        var id = Guid.NewGuid();
        service.Items.Add(new StudyPlanResponse(id, "Intensivo", 1.25m, StudyPlanStatus.Active));
        var controller = new StudyPlansController(service);

        var result = await controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudyPlanResponse>(ok.Value);

        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task UpdateShouldReturnOk()
    {
        var service = new FakeStudyPlanService();
        var id = Guid.NewGuid();
        var controller = new StudyPlansController(service);

        var result = await controller.Update(
            id,
            new UpdateStudyPlanRequest("Leve", 0.75m),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudyPlanResponse>(ok.Value);

        Assert.Equal(id, response.Id);
        Assert.Equal("Leve", response.Name);
        Assert.Equal(0.75m, response.Coefficient);
    }

    [Fact]
    public async Task ChangeStatusShouldReturnOk()
    {
        var service = new FakeStudyPlanService();
        var id = Guid.NewGuid();
        var controller = new StudyPlansController(service);

        var result = await controller.ChangeStatus(
            id,
            new ChangeStudyPlanStatusRequest(StudyPlanStatus.Inactive),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudyPlanResponse>(ok.Value);

        Assert.Equal(id, response.Id);
        Assert.Equal(StudyPlanStatus.Inactive, response.Status);
    }

    private sealed class FakeStudyPlanService : IStudyPlanService
    {
        public List<StudyPlanResponse> Items { get; } = [];

        public Task<StudyPlanResponse> CreateAsync(
            CreateStudyPlanRequest request,
            CancellationToken cancellationToken)
        {
            var response = new StudyPlanResponse(
                Guid.NewGuid(),
                request.Name?.Trim() ?? string.Empty,
                request.Coefficient,
                StudyPlanStatus.Active);

            Items.Add(response);
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<StudyPlanResponse>> ListAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<StudyPlanResponse>>(Items);

        public Task<StudyPlanResponse> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
            => Task.FromResult(
                Items.FirstOrDefault(x => x.Id == id)
                ?? new StudyPlanResponse(id, "Intensivo", 1.25m, StudyPlanStatus.Active));

        public Task<StudyPlanResponse> UpdateAsync(
            Guid id,
            UpdateStudyPlanRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new StudyPlanResponse(
                id,
                request.Name?.Trim() ?? string.Empty,
                request.Coefficient,
                StudyPlanStatus.Active));

        public Task<StudyPlanResponse> ChangeStatusAsync(
            Guid id,
            ChangeStudyPlanStatusRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new StudyPlanResponse(
                id,
                "Intensivo",
                1.25m,
                request.Status));
    }
}