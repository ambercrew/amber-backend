using Amber.Application.Sync.Commands;
using Amber.Application.Sync.Dto;
using Amber.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;
using Amber.WebApi.Extensions;
using Amber.WebApi.Sync.Protos;
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
    [HttpGet]
    [ProducesResponseType<SyncedEntitiesPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> GetSyncedEntitiesAfterOrderedByCreatedDateAsync(
        [FromQuery] DateTime date,
        [FromQuery] int page,
        CancellationToken cancellationToken
    )
    {
        var userId = this.GetSignedInUserId();
        var dto = await queryMediator.QueryAsync(
            new GetSyncedEntitiesAfterOrderedByCreatedDateQuery(date, page, userId),
            cancellationToken
        );
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> SyncEntitiesAsync(
        List<SyncEntityDto> dto,
        CancellationToken cancellationToken
    )
    {
        var userId = this.GetSignedInUserId();
        var username = this.GetSignedInUserUsername();
        await commandMediator.SendAsync(
            new SyncEntitiesCommand(dto, userId, username),
            cancellationToken
        );
        return Ok();
    }

    [HttpPost]
    [Consumes("application/x-protobuf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> ReceiveChangeBatchAsync(CancellationToken cancellationToken)
    {
        // TODO: move this into other methods and fix data model and all
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        _ = ChangeBatch.Parser.ParseFrom(memoryStream.ToArray());

        return Ok();
    }
}
