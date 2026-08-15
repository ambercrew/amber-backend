using Amber.Domain.Users.Entities;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Users.Services;

public class UserEmailVerificationCodeSender(IEmailService emailService, ILogger<UserEmailVerificationCodeSender> logger)
    : IUserEmailVerificationCodeSender
{
    public async Task SendVerificationEmailAsync(User user)
    {
        logger.LogInformation(
            "Sending verification code to user with email '{Email}'.",
            user.Email
        );

#if DEBUG
        logger.LogInformation(
            "Email verification code for user is '{EmailVerificationCode}'.",
            user.EmailVerificationCode.Value
        );
#endif

        await emailService.SendEmailFromTemplateAsync(
            user.Email,
            "Amber: Your Email Verification Code",
            "EmailVerification.html",
            ("VerificationCode", user.EmailVerificationCode.Value)
        );
    }
}
