using Brainy.Application.Services;
using Brainy.Application.Users.Dto;
using Brainy.Application.Users.Services;
using Brainy.Domain.Users.Entities;
using Brainy.Domain.Users.Repositories;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Users.Commands.SignUpUser;

public class SignUpUserCommandHandler(
    IUserRepository userRepository,
    IUserEmailVerificationCodeSender userEmailVerificationCodeSender,
    IRandomGenerator randomGenerator,
    ILogger<SignUpUserCommandHandler> logger
) : ICommandHandler<SignUpUserCommand, UserInformationDto>
{
    public async Task<UserInformationDto> HandleAsync(
        SignUpUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var email = new Email(command.SignUpDto.Email);
        var username = new Username(command.SignUpDto.Username);

        if (
            await userRepository.IsEmailUsedAsync(email)
            || await userRepository.IsUsernameUsedAsync(username)
        )
        {
            throw new InvalidOperationException("The email or username is already used!");
        }

        var plainPassword = new PlainPassword(command.SignUpDto.Password);
        var hashedPassword = new HashedPassword(plainPassword);
        var emailVerificationCode = new EmailVerificationCode(
            randomGenerator.GenerateRandomAlphanumeric(EmailVerificationCode.MaxLength)
        );

        var user = new User(
            id: Guid.NewGuid(),
            username: username,
            email: email,
            firstName: command.SignUpDto.FirstName,
            lastName: command.SignUpDto.LastName,
            password: hashedPassword,
            signOutDate: DateTime.UtcNow,
            isEmailVerified: false,
            emailVerificationCode: emailVerificationCode,
            lastDateTimeOfSentVerificationCode: DateTime.UtcNow,
            registrationDate: DateTime.UtcNow
        );

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        logger.LogInformation(
            "User with username '{Username}' and email '{Email}' signed-up",
            command.SignUpDto.Username,
            command.SignUpDto.Email
        );

        await userEmailVerificationCodeSender.SendVerificationEmailAsync(user);
        return UserInformationDto.FromUser(user);
    }
}
