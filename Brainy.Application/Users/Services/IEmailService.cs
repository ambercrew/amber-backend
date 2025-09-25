using Brainy.Application.Services;
using Brainy.Domain.Users.ValueObjects;

namespace Brainy.Application.Users.Services;

public interface IEmailService : IApplicationService
{
    /// <summary>
    /// Sends an email from one of the saved templates in the static files folder
    /// in the domains project.
    /// </summary>
    Task SendEmailFromTemplateAsync(
        Email email,
        string subject,
        string templateName,
        params (string placeholderName, string value)[] formatArgs
    );
}
