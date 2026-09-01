using Microsoft.AspNetCore.Mvc;
using StudyTime.Application.StudyPlans;

namespace StudyTime.Api.Controllers;

[ApiController]
[Route("api/study-plans")]
public sealed class StudyPlansController(IStudyPlanService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudyPlanResponse>> Create(
        [FromBody] CreateStudyPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudyPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudyPlanResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanResponse>> Update(
        Guid id,
        [FromBody] UpdateStudyPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanResponse>> ChangeStatus(
        Guid id,
        [FromBody] ChangeStudyPlanStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ChangeStatusAsync(id, request, cancellationToken);

        return Ok(result);
    }
}