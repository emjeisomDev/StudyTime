using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Application.Dtos;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.StudyAreaWeeks.Commands;
using StudyTime.Application.StudyAreaWeeks.Queries;

namespace StudyTime.Api.Controllers;

/**
//     POST	/study-area-weeks	        201 Created
//     POST	/study-area-weeks/batch	    201 Created
//     GET	/study-area-weeks/{id}	    200 OK
//     PUT	/study-area-weeks/{id}	    200 OK
//     DELETE	/study-area-weeks/{id}	204 No Content
**/
[ApiController]
[Route("api/v1/study-area-weeks")]
[Produces("application/json")]
public sealed class StudyAreaWeeksController(IMediator mediator, IMapper mapper) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Cria uma configuração semanal individual.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyAreaWeekResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StudyAreaWeekResponse>> Create(
        [FromBody] CreateStudyAreaWeekRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(
            new CreateStudyAreaWeekCommand(
                request.StudyAreaId,
                request.StudyPlanId,
                request.WeekStartDate),
            cancellationToken);

        var response = _mapper.Map<StudyAreaWeekResponse>(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Cria várias configurações para a mesma semana em uma única operação.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<StudyAreaWeekResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<StudyAreaWeekResponse>>> CreateBatch(
        [FromBody] CreateStudyAreaWeeksBatchRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(item => new StudyAreaWeekBatchItemDto(
                item.StudyAreaId,
                item.StudyPlanId))
            .ToArray();

        var result = await _mediator.Send<IReadOnlyList<StudyAreaWeekDto>>(
            new CreateStudyAreaWeeksBatchCommand(
                request.WeekStartDate,
                items),
            cancellationToken);

        var responses = _mapper
            .Map<IReadOnlyList<StudyAreaWeekResponse>>(result);

        if (responses.Count == 0)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                responses);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = responses[0].Id },
            responses);
    }

    /// <summary>
    /// Retorna uma configuração semanal pelo identificador.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudyAreaWeekResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StudyAreaWeekResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        var dto = await _mediator.Send(
            new GetStudyAreaWeekByIdQuery(id),
            cancellationToken);

        return Ok(_mapper.Map<StudyAreaWeekResponse>(dto));
    }

    /// <summary>
    /// Atualiza a área e/ou o plano associados à configuração semanal.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudyAreaWeekResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StudyAreaWeekResponse>> Update(
        Guid id,
        [FromBody] UpdateStudyAreaWeekRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        var result = await _mediator.Send<StudyAreaWeekDto>(
            new UpdateStudyAreaWeekCommand(
                id,
                request.StudyAreaId,
                request.StudyPlanId),
            cancellationToken);

        return Ok(_mapper.Map<StudyAreaWeekResponse>(result));
    }

    /// <summary>
    /// Remove uma configuração semanal.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        await _mediator.Send(
            new DeleteStudyAreaWeekCommand(id),
            cancellationToken);

        return NoContent();
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
    }
}
