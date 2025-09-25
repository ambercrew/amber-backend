using Brainy.Domain.Users.Repositories;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Users.Queries.AreCredentialsValid;

public class AreCredentialsValidQueryHandler(
    IUserRepository userRepository,
    ILogger<AreCredentialsValidQueryHandler> logger
) : IQueryHandler<AreCredentialsValidQuery, bool>
{
    public async Task<bool> HandleAsync(
        AreCredentialsValidQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var username = new Username(query.SignInDto.Username);
        var user = await userRepository.GetUserByUsernameIfExistsAsync(username);
        if (user is null)
        {
            logger.LogInformation("The user with username '{Username}' was not found.", username);
            return false;
        }

        if (user.Password.VerifyEqual(new PlainPassword(query.SignInDto.Password)))
        {
            return true;
        }

        logger.LogInformation(
            "Wrong password provided for user with username '{Username}'.",
            username
        );
        return false;
    }
}
