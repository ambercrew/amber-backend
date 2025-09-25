using Brainy.Application.Users.Services;
using Brainy.Domain.Users.Repositories;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    IUserDeletionEmailSender userDeletionEmailSender,
    ILogger<DeleteUserCommandHandler> logger
) : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(
        DeleteUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Deleting user with id {Id}.", command.UserId);

        var user = await userRepository.GetUserByIdAsync(command.UserId);

        var count = await userRepository.DeleteUserByIdAsync(command.UserId);
        if (count == 0)
        {
            throw new InvalidOperationException("User does not exist.");
        }

        await userDeletionEmailSender.SendDeletionEmailAsync(user);
    }
}
