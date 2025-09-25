using Brainy.Application.Users.Dto;
using Brainy.Domain.Users.Repositories;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Users.Commands.UpdatePassword;

public class UpdatePasswordCommandHandler(
    IUserRepository userRepository,
    ILogger<UpdatePasswordCommandHandler> logger
) : ICommandHandler<UpdatePasswordCommand, UserInformationDto>
{
    public async Task<UserInformationDto> HandleAsync(
        UpdatePasswordCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userRepository.GetUserByUsernameAsync(command.Username);

        if (!user.Password.VerifyEqual(new PlainPassword(command.UpdatePasswordDto.OldPassword)))
        {
            throw new InvalidOperationException("Incorrect password!");
        }

        var hashedPassword = new HashedPassword(
            new PlainPassword(command.UpdatePasswordDto.NewPassword)
        );
        user.Password = hashedPassword;
        user.SignOutDate = DateTime.UtcNow;

        await userRepository.SaveChangesAsync();
        logger.LogInformation("Updated password for '{Username}'.", command.Username.Value);

        return UserInformationDto.FromUser(user);
    }
}
