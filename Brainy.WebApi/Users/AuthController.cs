using Brainy.Application.Queries.AreCredentialsValid;
using Brainy.Application.Users.Commands.ResendEmailVerificationCode;
using Brainy.Application.Users.Commands.SignOutUser;
using Brainy.Application.Users.Commands.SignUpUser;
using Brainy.Application.Users.Commands.UpdatePassword;
using Brainy.Application.Users.Commands.VerifyUserEmail;
using Brainy.Application.Users.Dto;
using Brainy.Application.Users.Queries.AreCredentialsValid;
using Brainy.Application.Users.Queries.GetUserByUsername;
using Brainy.Domain.Users.ValueObjects;
using Brainy.WebApi.Extensions;
using Brainy.WebApi.Users.Dto;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brainy.WebApi.Users;

[ApiController]
[Route(Constants.RouteTemplate)]
[EnableRateLimiting(Constants.Policies.AuthRateLimitingPolicyName)]
public class AuthController(
    ILogger<AuthController> logger,
    ICommandMediator commandMediator,
    IQueryMediator queryMediator,
    JwtTokenService jwtTokenService
) : ControllerBase
{
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [ProducesResponseType<SignInResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> SignUpAsync(SignUpDto signUpDto)
    {
        var user = await commandMediator.SendAsync(new SignUpUserCommand(signUpDto));
        return Ok(CreateSignInResponse(user));
    }

    [HttpPost("update-password")]
    [Authorize]
    [ProducesResponseType<SignInResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePasswordAsync(UpdatePasswordDto updatePasswordDto)
    {
        var username = this.GetSignedInUserUsername();
        var user = await commandMediator.SendAsync(
            new UpdatePasswordCommand(updatePasswordDto, username)
        );

        // Issuing a new token since sign-out date is updated.
        return Ok(CreateSignInResponse(user));
    }

    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType<SignInResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignInAsync(SignInDto signInDto)
    {
        var validCredentials = await queryMediator.QueryAsync(
            new AreCredentialsValidQuery(signInDto)
        );
        if (validCredentials)
        {
            var user = await queryMediator.QueryAsync(
                new GetUserByUsernameQuery(new Username(signInDto.Username))
            );
            return Ok(CreateSignInResponse(user));
        }

        return Unauthorized();
    }

    [Authorize]
    [HttpPost("verify-email")]
    [ProducesResponseType<SignInResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> VerifyUserEmailAsync(VerifyEmailDto verifyEmailDto)
    {
        var username = this.GetSignedInUserUsername();
        await commandMediator.SendAsync(new VerifyUserEmailCommand(username, verifyEmailDto));

        // Issuing a new token to give the user the correct role.
        var user = await queryMediator.QueryAsync(new GetUserByUsernameQuery(username));
        return Ok(CreateSignInResponse(user));
    }

    private SignInResponseDto CreateSignInResponse(UserInformationDto user)
    {
        var token = jwtTokenService.CreateToken(user);
        logger.LogInformation("User with username '{Username}' signed-in.", user.Username);
        return new SignInResponseDto(user, token);
    }

    [Authorize]
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public async Task<IActionResult> ResendEmailVerificationCodeAsync()
    {
        var username = this.GetSignedInUserUsername();
        await commandMediator.SendAsync(new ResendEmailVerificationCodeCommand(username));
        return Ok();
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOutAsync()
    {
        var username = this.GetSignedInUserUsername();
        await commandMediator.SendAsync(new SignOutUserCommand(username));

        logger.LogInformation("User with username '{Username}' signed-out.", username);

        return Ok();
    }
}
