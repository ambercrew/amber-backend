using Amber.Application.Users.Commands.DeleteUser;
using Amber.Application.Users.Commands.UpdateUserInformation;
using Amber.Application.Users.Dto;
using Amber.Application.Users.Queries.GetUserByUsername;
using Amber.WebApi.Extensions;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amber.WebApi.Users;

[ApiController]
[Authorize]
[Route(Constants.RouteTemplate)]
public class UserController(IQueryMediator queryMediator, ICommandMediator commandMediator)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserInformationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserInformationAsync()
    {
        var dto = await queryMediator.QueryAsync(
            new GetUserByUsernameQuery(this.GetSignedInUserUsername())
        );
        return Ok(dto);
    }

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserInformationAsync(UpdateUserInformationDto dto)
    {
        await commandMediator.SendAsync(new UpdateUserCommand(dto, this.GetSignedInUserUsername()));
        return Ok();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUserAsync()
    {
        var userId = this.GetSignedInUserId();
        await commandMediator.SendAsync(new DeleteUserCommand(userId));

        return Ok();
    }
}
