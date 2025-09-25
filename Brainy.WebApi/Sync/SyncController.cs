using Brainy.Application.Sync.Commands;
using Brainy.Application.Sync.Dto;
using Brainy.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;
using Brainy.WebApi.Extensions;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brainy.WebApi.Sync;

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
}
