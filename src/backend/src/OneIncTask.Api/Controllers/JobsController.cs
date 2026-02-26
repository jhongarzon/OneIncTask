using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Common;
using static OneIncTask.Application.Features.Jobs.Commands.StartJob;
using static OneIncTask.Application.Features.Jobs.Commands.CancelJob;
using static OneIncTask.Application.Features.Jobs.Queries.GetJobStatus;
using static OneIncTask.Application.Features.Jobs.Queries.GetJobHistory;

namespace OneIncTask.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher) : ControllerBase
{
    private string UserId => User.Identity?.Name ?? throw new UnauthorizedAccessException();

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> StartJob([FromBody] StartJobRequestDto dto, CancellationToken cancellationToken)
    {
        var command = new StartJobRequest(UserId, dto.InputText);
        var response = await commandDispatcher.SendAsync(command, cancellationToken);

        return response.IsSuccess
            ? Results.Accepted($"/api/jobs/{response.Data!.JobId}/status", response.Data)
            : response.ToResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CancelJob(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelJobRequest(id, UserId);
        var response = await commandDispatcher.SendAsync(command, cancellationToken);
        return response.ToResult();
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetJobStatus(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetJobStatusRequest(id, UserId);
        var result = await queryDispatcher.QueryAsync(query, cancellationToken);

        return result != null
            ? Results.Ok(result)
            : Results.NotFound($"Job {id} not found.");
    }

    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> GetJobHistory(CancellationToken cancellationToken)
    {
        var query = new GetJobHistoryRequest(UserId);
        var result = await queryDispatcher.QueryAsync(query, cancellationToken);
        return Results.Ok(result);
    }
}

public record StartJobRequestDto(string InputText);
