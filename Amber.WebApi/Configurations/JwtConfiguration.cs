namespace Amber.WebApi.Configurations;

public class JwtConfiguration
{
    public string SecretKey { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public int ExpiryInDays { get; init; }
}
