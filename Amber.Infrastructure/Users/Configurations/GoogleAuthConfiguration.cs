namespace Amber.Infrastructure.Users.Configurations;

public class GoogleAuthConfiguration
{
    /// <summary>The OAuth 2.0 client ID of the frontend application, used to validate the "aud" claim.</summary>
    public string ClientId { get; init; } = null!;
}
