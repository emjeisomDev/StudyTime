using MediatR;
using AutoMapper;
using StudyTime.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using StudyTime.Domain.Exceptions;
using StudyTime.Api.Contracts.Requests;
using StudyTime.Api.Contracts.Responses;
using StudyTime.Application.StudyPlans.Queries;
using StudyTime.Application.StudyPlans.Commands;

namespace StudyTime.Api.Controllers;


/**
//     GET /study-plans/{id}: returns 200 OK or 404 Not Found.
//     POST /study-plans: creates the plan with a positive coefficient and returns 201 Created.
//     PATCH /study-plans/{id}/status: accepts only "active" or "inactive"; allows bidirectional 
//     transitions and returns 200 OK.
//     The status response is validated as exact text, ignoring only case; numeric values ​​or other 
//     states are not accepted.
**/
[ApiController]
[Route("api/v1/study-plans")]
[Produces("application/json")]
public sealed class StudyPlansController(IMediator mediator, IMapper mapper) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    [HttpPost]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StudyPlanResponse>> Create(
        [FromBody] CreateStudyPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(
                new CreateStudyPlanCommand(
                    request.Name,
                    request.Coefficient),
                cancellationToken);

            var response = _mapper.Map<StudyPlanResponse>(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (DomainException exception)
        {
            return UnprocessableEntity(CreateProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Study plan validation failed",
                exception.Message));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(CreateProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Study plan validation failed",
                exception.Message));
        }
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StudyPlanResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateStudyPlanStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid study plan identifier",
                "The study plan identifier cannot be empty."));
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Status))
        {
            return UnprocessableEntity(CreateProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Invalid study plan status",
                "Status must be either 'active' or 'inactive'."));
        }

        StudyPlanStatus status;

        if (string.Equals(
            request.Status,
            "active",
            StringComparison.OrdinalIgnoreCase))
        {
            status = StudyPlanStatus.Active;
        }
        else if (string.Equals(
            request.Status,
            "inactive",
            StringComparison.OrdinalIgnoreCase))
        {
            status = StudyPlanStatus.Inactive;
        }
        else
        {
            return UnprocessableEntity(CreateProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Invalid study plan status",
                "Status must be either 'active' or 'inactive'."));
        }

        try
        {
            await _mediator.Send(
                new UpdateStudyPlanStatusCommand(id, status),
                cancellationToken);

            var dto = await _mediator.Send(
                new GetStudyPlanByIdQuery(id),
                cancellationToken);

            if (dto is null)
            {
                return NotFound(CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Study plan not found",
                    $"StudyPlan with id '{id}' was not found."));
            }

            return Ok(_mapper.Map<StudyPlanResponse>(dto));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Study plan not found",
                exception.Message));
        }
        catch (DomainException exception)
        {
            return UnprocessableEntity(CreateProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Study plan validation failed",
                exception.Message));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(CreateProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Study plan validation failed",
                exception.Message));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudyPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid study plan identifier",
                "The study plan identifier cannot be empty."));
        }

        var dto = await _mediator.Send(
            new GetStudyPlanByIdQuery(id),
            cancellationToken);

        if (dto is null)
        {
            return NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Study plan not found",
                $"StudyPlan with id '{id}' was not found."));
        }

        return Ok(_mapper.Map<StudyPlanResponse>(dto));
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

public sealed record UpdateStudyPlanStatusRequest(string? Status);
