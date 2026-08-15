using AsyncKeyedLock;
using Amber.Application.Users.Services;
using Amber.Domain.Users.Entities;
using Amber.Domain.Users.Repositories;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.ResendEmailVerificationCode;

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
