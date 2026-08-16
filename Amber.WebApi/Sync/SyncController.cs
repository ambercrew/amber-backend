using Amber.Application.Sync.Commands;
using Amber.Application.Sync.Dto;
using Amber.Application.Sync.Queries.PullChangesQuery;
using Amber.WebApi.Extensions;
using Amber.WebApi.Sync.Protos;
using Google.Protobuf;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amber.WebApi.Sync;

[Authorize(Roles = Constants.Roles.VerifiedUser)]
[ApiController]
[Route(Constants.RouteTemplate)]
public class SyncController(IQueryMediator queryMediator, ICommandMediator commandMediator)
    : ControllerBase
{
    [HttpPost("push")]
    [Consumes("application/x-protobuf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status507InsufficientStorage)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> PushAsync(CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        var batch = ChangeBatch.Parser.ParseFrom(memoryStream.ToArray());

        var userId = this.GetSignedInUserId();
        var username = this.GetSignedInUserUsername();

        var cells = batch
            .Cells.Select(cell => new CellChangeDto(
                cell.Tbl,
                cell.RowId,
                cell.Col,
                cell.HasValue ? cell.Value.ToByteArray() : null,
                cell.Hlc,
                cell.DeviceId
            ))
            .ToList();

        await commandMediator.SendAsync(
            new PushChangesCommand(cells, userId, username),
            cancellationToken
        );

        return Ok();
    }

    [HttpGet("pull")]
    [Produces("application/x-protobuf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> PullAsync(
        [FromQuery] long sinceServerSeq,
        CancellationToken cancellationToken
    )
    {
        var userId = this.GetSignedInUserId();

        var page = await queryMediator.QueryAsync(
            new PullChangesQuery(sinceServerSeq, userId),
            cancellationToken
        );

        var response = new PullResponse
        {
            NextServerSeq = page.NextServerSeq,
            HasMore = page.HasMore,
        };
        response.Cells.AddRange(
            page.Cells.Select(cell => new CellChange
            {
                Tbl = cell.Table,
                RowId = cell.RowId,
                Col = cell.Column,
                Value = cell.Value is null ? null : ByteString.CopyFrom(cell.Value),
                Hlc = cell.Hlc,
                DeviceId = cell.DeviceId,
            })
        );

        return File(response.ToByteArray(), "application/x-protobuf");
    }
}
