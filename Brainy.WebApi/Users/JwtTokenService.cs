using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Brainy.Application.Users.Dto;
using Brainy.WebApi.Configurations;
using Microsoft.IdentityModel.Tokens;

namespace Brainy.WebApi.Users;

public class JwtTokenService(JwtConfiguration jwtConfiguration)
{
    /// <summary>
    /// Custom claim type used to validate the token against the user's sign-out date, since
    /// the standard "iat" claim is not reliably round-tripped through the default inbound
    /// claim type mapping.
    /// </summary>
    public const string IssuedAtClaimType = "issued_at";

    public string CreateToken(UserInformationDto user)
    {
        IList<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        ];

        if (user.IsEmailVerified)
        {
            claims.Add(new Claim(ClaimTypes.Role, Constants.Roles.VerifiedUser));
        }

        // Adding one second so that the issued utc is always bigger than
        // the sign-out date on registration.
        var issuedAt = DateTime.UtcNow.AddSeconds(1);
        claims.Add(new Claim(IssuedAtClaimType, issuedAt.ToString("o")));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.SecretKey)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: jwtConfiguration.Issuer,
            audience: jwtConfiguration.Audience,
            claims: claims,
            expires: issuedAt.AddDays(jwtConfiguration.ExpiryInDays),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
