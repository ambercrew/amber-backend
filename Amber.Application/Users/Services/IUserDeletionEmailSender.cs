using Amber.Application.Services;
using Amber.Domain.Users.Entities;

namespace Amber.Application.Users.Services;

public interface IUserDeletionEmailSender : IApplicationService
{
    Task SendDeletionEmailAsync(User user);
}
