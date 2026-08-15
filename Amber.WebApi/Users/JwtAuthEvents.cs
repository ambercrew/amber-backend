using System.Globalization;
using System.Security.Claims;
using Amber.Domain.Users.Repositories;
using Amber.Domain.Users.ValueObjects;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Amber.WebApi.Users;

/// <summary>
/// Validates that the token was not issued before user sign-out date and if so it rejects the
/// token.
/// </summary>
public class JwtAuthEvents(IUserRepository userRepository, ILogger<JwtAuthEvents> logger)
    : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var usernameValue = context
            .Principal?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)
            ?.Value;

        if (string.IsNullOrEmpty(usernameValue))
        {
            logger.LogInformation("No username is in the token!");
            context.Fail("Invalid token.");
            return;
        }

        var username = new Username(usernameValue);

        if (!await userRepository.IsUsernameUsedAsync(username))
        {
            logger.LogInformation("Cannot find the user with username '{Username}'", usernameValue);
            context.Fail("Invalid token.");
            return;
        }

        var issuedAtValue = context
            .Principal?.Claims?.FirstOrDefault(x => x.Type == JwtTokenService.IssuedAtClaimType)
            ?.Value;

        if (
            string.IsNullOrEmpty(issuedAtValue)
            || !DateTime.TryParse(
                issuedAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var issuedAt
            )
        )
        {
            logger.LogInformation("The issued datetime is null!");
            context.Fail("Invalid token.");
            return;
        }

        var signOutDateTime = await userRepository.GetUserSignOutDateTimeAsync(username);

        if (signOutDateTime > issuedAt)
        {
            logger.LogInformation(
                "Rejecting the token since the user signed-out after issuing it!"
            );
            context.Fail("Invalid token.");
        }
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        logger.LogInformation(
            "The user is trying to access an authorized endpoint ({endpoint}) while the user is not authenticated!",
            context.Request.Path
        );
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
