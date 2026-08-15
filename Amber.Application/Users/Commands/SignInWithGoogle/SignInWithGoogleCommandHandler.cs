using System.Text.RegularExpressions;
using Amber.Application.Services;
using Amber.Application.Users.Dto;
using Amber.Application.Users.Services;
using Amber.Domain.Users.Entities;
using Amber.Domain.Users.Repositories;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Users.Commands.SignInWithGoogle;

public partial class SignInWithGoogleCommandHandler(
    IUserRepository userRepository,
    IGoogleTokenVerifier googleTokenVerifier,
    IRandomGenerator randomGenerator,
    ILogger<SignInWithGoogleCommandHandler> logger
) : ICommandHandler<SignInWithGoogleCommand, UserInformationDto>
{
    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex InvalidUsernameCharactersRegex();

    public async Task<UserInformationDto> HandleAsync(
        SignInWithGoogleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var googleUserInfo = await googleTokenVerifier.VerifyAsync(command.GoogleSignInDto.IdToken);
        if (googleUserInfo is null)
        {
            throw new InvalidOperationException("Invalid Google token!");
        }

        var existingUserByGoogleId = await userRepository.GetUserByGoogleIdIfExistsAsync(
            googleUserInfo.GoogleId
        );
        if (existingUserByGoogleId is not null)
        {
            return UserInformationDto.FromUser(existingUserByGoogleId);
        }

        var email = new Email(googleUserInfo.Email);
        var existingUserByEmail = await userRepository.GetUserByEmailIfExistsAsync(email);
        if (existingUserByEmail is not null)
        {
            if (!existingUserByEmail.IsEmailVerified)
            {
                throw new InvalidOperationException(
                    "An account with this email already exists but is not verified. Please verify your email before signing in with Google."
                );
            }

            existingUserByEmail.LinkGoogleAccount(googleUserInfo.GoogleId);
            await userRepository.SaveChangesAsync();

            logger.LogInformation(
                "Linked Google account to existing user with username '{Username}'.",
                existingUserByEmail.Username.Value
            );
            return UserInformationDto.FromUser(existingUserByEmail);
        }

        var username = await GenerateUniqueUsernameAsync(googleUserInfo.Email);
        var emailVerificationCode = new EmailVerificationCode(
            randomGenerator.GenerateRandomAlphanumeric(EmailVerificationCode.MaxLength)
        );

        var user = new User(
            id: Guid.NewGuid(),
            username: username,
            email: email,
            firstName: string.IsNullOrWhiteSpace(googleUserInfo.FirstName)
                ? "N/A"
                : googleUserInfo.FirstName,
            lastName: string.IsNullOrWhiteSpace(googleUserInfo.LastName)
                ? "N/A"
                : googleUserInfo.LastName,
            password: null,
            signOutDate: DateTime.UtcNow,
            isEmailVerified: true,
            emailVerificationCode: emailVerificationCode,
            lastDateTimeOfSentVerificationCode: DateTime.UtcNow,
            registrationDate: DateTime.UtcNow,
            googleId: googleUserInfo.GoogleId
        );

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        logger.LogInformation(
            "User with username '{Username}' and email '{Email}' signed-up via Google.",
            username.Value,
            googleUserInfo.Email
        );

        return UserInformationDto.FromUser(user);
    }

    private async Task<Username> GenerateUniqueUsernameAsync(string email)
    {
        var localPart = email.Split('@')[0];
        var sanitized = InvalidUsernameCharactersRegex().Replace(localPart, "").ToLowerInvariant();
        if (sanitized.Length < 3)
        {
            sanitized = $"{sanitized}user";
        }
        sanitized = sanitized[..Math.Min(sanitized.Length, 24)];

        var candidate = new Username(sanitized);
        while (await userRepository.IsUsernameUsedAsync(candidate))
        {
            var suffix = randomGenerator.GenerateRandomAlphanumeric(6);
            candidate = new Username($"{sanitized}{suffix}");
        }

        return candidate;
    }
}
