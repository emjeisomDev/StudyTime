using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Application.Dtos;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Api.Controllers;

[ApiController]
[Route("bootstrap")]
[Produces("application/json")]
public sealed class BootstrapController(IMediator mediator, IMapper mapper) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Inicializa o sistema com a primeira configuração semanal.
    /// </summary>
    /// 
    /// POST	/bootstrap	201 Created
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<StudyAreaWeekResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<StudyAreaWeekResponse>>> Create(
        [FromBody] BootstrapRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(item => new StudyAreaWeekBatchItemDto(
                item.StudyAreaId,
                item.StudyPlanId))
            .ToArray();

        var dtos = await _mediator.Send(
            new BootstrapSystemCommand(items),
            cancellationToken);

        var responses = _mapper
            .Map<IReadOnlyList<StudyAreaWeekResponse>>(dtos);

        return StatusCode(
            StatusCodes.Status201Created,
            responses);
    }
}
