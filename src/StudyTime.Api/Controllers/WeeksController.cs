using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.WeeklyAssessments.Queries;

namespace StudyTime.Api.Controllers;

[ApiController]
[Route("weeks")]
[Produces("application/json")]
public sealed class WeeksController(IMediator mediator, IMapper mapper) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Retrieves the consolidated summary of the previous week.
    /// </summary>
    [HttpGet("previous-summary")]
    [ProducesResponseType(typeof(PreviousWeekSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PreviousWeekSummaryResponse>> GetPreviousSummary(CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetPreviousWeekSummaryQuery(), cancellationToken);
            var response = _mapper.Map<PreviousWeekSummaryResponse>(dto);
            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Previous week summary not found",
                Detail = exception.Message
            });
        }
    }
}
