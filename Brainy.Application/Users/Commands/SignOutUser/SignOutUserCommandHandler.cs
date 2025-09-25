using Brainy.Domain.Users.Repositories;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.SignOutUser;

public class SignOutUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<SignOutUserCommand>
{
    public async Task HandleAsync(
        SignOutUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userRepository.GetUserByUsernameAsync(command.Username);
        user.SignOutDate = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();
    }
}
