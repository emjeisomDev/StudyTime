using Microsoft.AspNetCore.Mvc;
using StudyTime.Application.StudyAreaWeeks;

namespace StudyTime.Api.Controllers;

[ApiController]
[Route("api/study-area-weeks")]
public sealed class StudyAreaWeeksController(IStudyAreaWeekService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(StudyAreaWeekResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudyAreaWeekResponse>> Create(
        [FromBody] CreateStudyAreaWeekRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, cancellationToken);
        return Created($"/study-area-weeks/{response.Id}", response);
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(StudyAreaWeekBatchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudyAreaWeekBatchResponse>> CreateBatch(
        [FromBody] CreateStudyAreaWeekBatchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateBatchAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudyAreaWeekResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudyAreaWeekResponse>> Update(
        Guid id,
        [FromBody] UpdateStudyAreaWeekRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (response is null)
            return NotFound();

        return Ok(response);
    }

    [HttpGet("{id:guid}/assessment")]
    [ProducesResponseType(typeof(StudyAreaWeekAssessmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyAreaWeekAssessmentResponse>> GetAssessment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await service.GetAssessmentAsync(id, cancellationToken);
        if (response is null)
            return NotFound();

        return Ok(response);
    }
}