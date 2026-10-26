using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.StudyRecords.Commands;
using StudyTime.Application.StudyRecords.Queries;
using StudyTime.Api.Extensions;

namespace StudyTime.Api.Controllers;

/// <summary>
/// Exposes study-record operations for a study area week.
/// POST forwards creation to CreateStudyRecordCommand.
/// GET uses GetStudyRecordsByWeekQuery; chronological ordering remains the handler's responsibility.
/// DELETE .../last delegates selection and deletion to DeleteLastStudyRecordCommand.
/// KeyNotFoundException during query/delete operations returns 404 ProblemDetails.
/// Uncaught domain exceptions continue to be handled by the existing middleware.
/// </summary>
[ApiController]
[Route("study-area-weeks/{studyAreaWeekId:guid}/study-records")]
[Produces("application/json")]
public sealed class StudyRecordsController(IMediator mediator, IMapper mapper) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    [HttpPost]
    [ProducesResponseType(typeof(StudyRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudyRecordResponse>> Create(
        Guid studyAreaWeekId,
        [FromBody] CreateStudyRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            return BadRequest(this.CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        var dto = await _mediator.Send<StudyRecordDto>(
            new CreateStudyRecordCommand(
                studyAreaWeekId,
                request.Minutes),
            cancellationToken);

        var response = _mapper.Map<StudyRecordResponse>(dto);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudyRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StudyRecordResponse>>> GetByWeek(
        Guid studyAreaWeekId,
        CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            return BadRequest(this.CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        try
        {
            var dtos = await _mediator.Send(
                new GetStudyRecordsByWeekQuery(studyAreaWeekId),
                cancellationToken);

            var responses = _mapper.Map<IReadOnlyList<StudyRecordResponse>>(dtos);

            return Ok(responses);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(this.CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message));
        }
    }

    [HttpDelete("last")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteLast(
        Guid studyAreaWeekId,
        CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            return BadRequest(this.CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        try
        {
            // Force the response-bearing MediatR overload. 
            // ICommand also implements non-generic IRequest, so an untyped Send can select 
            // the wrong overload when its result is discarded.
            await _mediator.Send<Unit>(
                new DeleteLastStudyRecordCommand(studyAreaWeekId),
                cancellationToken);

            return NoContent();
        }
        catch (NoEligibleStudyRecordException exception)
        {
            return NotFound(this.CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(this.CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message));
        }
    }
}