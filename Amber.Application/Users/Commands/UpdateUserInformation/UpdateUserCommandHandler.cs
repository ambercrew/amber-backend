using Amber.Domain.Users.Repositories;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.UpdateUserInformation;

public class UpdateUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task HandleAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userRepository.GetUserByUsernameAsync(command.Username);
        if (!string.IsNullOrEmpty(command.UpdateUserInformationDto.FirstName))
            user.FirstName = command.UpdateUserInformationDto.FirstName;
        if (!string.IsNullOrEmpty(command.UpdateUserInformationDto.LastName))
            user.LastName = command.UpdateUserInformationDto.LastName;
        await userRepository.SaveChangesAsync();
    }
}
