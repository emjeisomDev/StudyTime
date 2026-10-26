using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Api.Extensions;
using StudyTime.Domain.Exceptions;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.StudyAreas.Commands;
using StudyTime.Application.StudyAreas.Queries;

namespace StudyTime.Api.Controllers;

/** 
//     GET /study-areas/{id}: retrieves by identifier and returns 200 OK or 404 Not Found.
//     POST /study-areas: creates an area and returns 201 Created.
//     PUT /study-areas/{id}: updates the name and default goal and returns 200 OK.
//     Name and minutes validation uses the existing CreateStudyAreaRequest contract, avoiding the 
//     creation of a third file.
**/
[ApiController]
[Route("study-areas")]
[Produces("application/json")]
public sealed class StudyAreasController(IMediator mediator, IMapper mapper) : ControllerBase
{

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudyAreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudyAreaResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(this.CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid study area identifier",
                "The study area identifier cannot be empty."));
        }

        try
        {
            var dto = await _mediator.Send(
                new GetStudyAreaByIdQuery(id),
                cancellationToken);

            return Ok(_mapper.Map<StudyAreaResponse>(dto));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(this.CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Study area not found",
                exception.Message));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudyAreaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StudyAreaResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var dtos = await _mediator.Send(new GetStudyAreasQuery(), cancellationToken);
        var responses = _mapper.Map<IReadOnlyList<StudyAreaResponse>>(dtos);
        return Ok(responses);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudyAreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudyAreaResponse>> Create(
        [FromBody] CreateStudyAreaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(
                new CreateStudyAreaCommand(
                    request.Name,
                    request.StdWeekStudyTime),
                cancellationToken);

            var response = _mapper.Map<StudyAreaResponse>(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (DomainException exception)
        {
            return UnprocessableEntity(this.CreateProblemDetails(
                StatusCodes.Status422UnprocessableEntity,
                "Study area validation failed",
                exception.Message));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(this.CreateProblemDetails(
                StatusCodes.Status422UnprocessableEntity,
                "Study area validation failed",
                exception.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudyAreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudyAreaResponse>> Update(
        Guid id,
        [FromBody] CreateStudyAreaRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(this.CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid study area identifier",
                "The study area identifier cannot be empty."));
        }

        try
        {
            var dto = await _mediator.Send(
                new UpdateStudyAreaCommand(
                    id,
                    request.Name,
                    request.StdWeekStudyTime),
                cancellationToken);

            return Ok(_mapper.Map<StudyAreaResponse>(dto));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(this.CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Study area not found",
                exception.Message));
        }
        catch (DomainException exception)
        {
            return UnprocessableEntity(this.CreateProblemDetails(
                StatusCodes.Status422UnprocessableEntity,
                "Study area validation failed",
                exception.Message));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(this.CreateProblemDetails(
                StatusCodes.Status422UnprocessableEntity,
                "Study area validation failed",
                exception.Message));
        }
    }

}
