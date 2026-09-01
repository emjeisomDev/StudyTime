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
}