using Amber.Domain.Users.Entities;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Users.Services;

public class UserDeletionEmailSender(
    IEmailService emailService,
    ILogger<UserDeletionEmailSender> logger
) : IUserDeletionEmailSender
{
    public async Task SendDeletionEmailAsync(User user)
    {
        logger.LogInformation(
            "Sending account deletion email to user with email '{Email}'.",
            user.Email
        );

        await emailService.SendEmailFromTemplateAsync(
            user.Email,
            "Amber: Your Account Has Been Deleted",
            "AccountDeletion.html",
            ("FirstName", user.FirstName)
        );
    }
}
