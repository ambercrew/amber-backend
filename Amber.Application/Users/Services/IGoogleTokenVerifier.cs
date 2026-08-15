using Amber.Application.Services;

namespace Amber.Application.Users.Services;

public interface IGoogleTokenVerifier : IApplicationService
{
    /// <summary>
    /// Verifies a Google-issued ID token and returns the identity it carries,
    /// or null if the token is missing, expired, or otherwise invalid.
    /// </summary>
    Task<GoogleUserInfo?> VerifyAsync(string idToken);
}
