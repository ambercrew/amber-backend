using Amber.Application.Users.Dto;
using Amber.Domain.Users.Repositories;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Users.Commands.UpdatePassword;

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

        // Users who signed up via Google have no password yet, so the first time they set
        // one there is nothing to verify against.
        if (
            user.Password is not null
            && !user.Password.VerifyEqual(new PlainPassword(command.UpdatePasswordDto.OldPassword))
        )
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
