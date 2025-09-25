using AsyncKeyedLock;
using Brainy.Application.Users.Services;
using Brainy.Domain.Users.Entities;
using Brainy.Domain.Users.Repositories;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.ResendEmailVerificationCode;

public class ResendEmailVerificationCodeCommandHandler(
    IUserEmailVerificationCodeSender userEmailVerificationCodeSender,
    IUserRepository userRepository,
    AsyncKeyedLocker<Username> asyncKeyedLocker
) : ICommandHandler<ResendEmailVerificationCodeCommand>
{
    public async Task HandleAsync(
        ResendEmailVerificationCodeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        using var releaser = await asyncKeyedLocker.LockOrNullAsync(
            command.Username,
            User.MinimumTimeBetweenResendingEmailVerificationCode
        );

        if (releaser is null)
            return;

        var user = await userRepository.GetUserByUsernameAsync(command.Username);

        if (user.RequestVerificationCodeResend(DateTime.UtcNow))
        {
            // Save changes before sending email.
            await userRepository.SaveChangesAsync();

            await userEmailVerificationCodeSender.SendVerificationEmailAsync(user);
        }
        else
        {
            throw new InvalidOperationException(
                "Please wait before requesting to send the verification code again."
            );
        }
    }
}
