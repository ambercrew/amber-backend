using Amber.Application.Users.Services;
using Amber.Domain.Users.ValueObjects;
using lettermint_dotnet.SendingApi;
using Microsoft.Extensions.Logging;

namespace Amber.Infrastructure.Users.Services;

public class EmailService(ILettermintSendingClient lettermintClient, ILogger<EmailService> logger)
    : IEmailService
{
    public async Task SendEmailFromTemplateAsync(
        Email email,
        string subject,
        string templateName,
        params (string placeholderName, string value)[] formatArgs
    )
    {
        var path = Path.Combine(
            Path.GetDirectoryName(typeof(IEmailService).Assembly.Location)!,
            "StaticFiles",
            "EmailTemplates",
            templateName
        );
        var html = await File.ReadAllTextAsync(path);

        foreach (var (placeholderName, value) in formatArgs)
        {
            html = html.Replace('{' + placeholderName + '}', value);
        }

#if DEBUG
        logger.LogInformation(
            "Sending email to {Email}\n{Subject}\n{Body}",
            email.Value,
            subject,
            html
        );
#endif

        await lettermintClient
            .Email.From("Amber", "no-reply@amberlearn.app")
            .To(email.Value)
            .Subject(subject)
            .SetHtmlBody(html)
            .SetRouteAsOutgoing()
            .SendAsync();

        logger.LogInformation("Email sent successfully to {Email}", email.Value);
    }
}
