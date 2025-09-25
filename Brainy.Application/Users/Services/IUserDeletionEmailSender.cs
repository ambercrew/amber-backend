using Brainy.Application.Services;
using Brainy.Domain.Users.Entities;

namespace Brainy.Application.Users.Services;

public interface IUserDeletionEmailSender : IApplicationService
{
    Task SendDeletionEmailAsync(User user);
}
