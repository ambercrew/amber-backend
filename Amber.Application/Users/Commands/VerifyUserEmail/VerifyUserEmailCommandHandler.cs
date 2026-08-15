using Amber.Domain.Users.Repositories;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Users.Commands.VerifyUserEmail;

public class VerifyUserEmailCommandHandler(
    IUserRepository userRepository,
    ILogger<VerifyUserEmailCommand> logger
) : ICommandHandler<VerifyUserEmailCommand>
{
    public async Task HandleAsync(
        VerifyUserEmailCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userRepository.GetUserByUsernameAsync(command.Username);

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Your email is already verified!");
        }

        if (
            user.EmailVerificationCode
            == new EmailVerificationCode(command.VerifyEmailDto.EmailVerificationCode)
        )
        {
            user.IsEmailVerified = true;
        }
        else
        {
            throw new InvalidOperationException("Incorrect verification code!");
        }

        await userRepository.SaveChangesAsync();
        logger.LogInformation(
            "User with username '{Username}' has verified email address.",
            command.Username.Value
        );
    }
}
