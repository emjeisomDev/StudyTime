using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.Dtos;
using StudyTime.Application.StudyRecords.Commands;
using StudyTime.Application.StudyRecords.Queries;

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
[Route("api/v1/study-area-weeks/{studyAreaWeekId:guid}/study-records")]
[Produces("application/json")]
public sealed class StudyRecordsController(
    IMediator mediator,
    IMapper mapper) : ControllerBase
{
    private readonly IMediator _mediator =
        mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly IMapper _mapper =
        mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Creates an immutable study record for a study area week.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StudyRecordResponse>> Create(
        Guid studyAreaWeekId,
        [FromBody] CreateStudyRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            return BadRequest(CreateProblem(
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

    /// <summary>
    /// Lists the study records associated with a study area week.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudyRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StudyRecordResponse>>> GetByWeek(
        Guid studyAreaWeekId,
        CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            return BadRequest(CreateProblem(
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
            return NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message));
        }
    }

    /// <summary>
    /// Deletes the last eligible study record using the application's LIFO rules.
    /// </summary>
    [HttpDelete("last")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteLast(
        Guid studyAreaWeekId,
        CancellationToken cancellationToken)
    {
        if (studyAreaWeekId == Guid.Empty)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid study area week identifier",
                "The study area week identifier cannot be empty."));
        }

        try
        {
            await _mediator.Send(
                new DeleteLastStudyRecordCommand(studyAreaWeekId),
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message));
        }
    }

    private static ProblemDetails CreateProblem(
        int status,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
    }
}
