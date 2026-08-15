using Amber.Application.Users.Services;
using Amber.Infrastructure.Users.Configurations;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;

namespace Amber.Infrastructure.Users.Services;

public class GoogleTokenVerifier(
    GoogleAuthConfiguration googleAuthConfiguration,
    ILogger<GoogleTokenVerifier> logger
) : IGoogleTokenVerifier
{
    public async Task<GoogleUserInfo?> VerifyAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [googleAuthConfiguration.ClientId],
                }
            );

            if (!payload.EmailVerified)
            {
                logger.LogInformation(
                    "Rejected Google sign-in for '{Email}' because the email is not verified.",
                    payload.Email
                );
                return null;
            }

            return new GoogleUserInfo(
                GoogleId: payload.Subject,
                Email: payload.Email,
                FirstName: payload.GivenName,
                LastName: payload.FamilyName
            );
        }
        catch (InvalidJwtException ex)
        {
            logger.LogInformation(ex, "Rejected invalid Google ID token.");
            return null;
        }
    }
}
