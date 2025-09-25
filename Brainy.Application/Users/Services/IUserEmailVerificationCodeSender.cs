using Brainy.Application.Services;
using Brainy.Domain.Users.Entities;

namespace Brainy.Application.Users.Services;

public interface IUserEmailVerificationCodeSender : IApplicationService
{
    Task SendVerificationEmailAsync(User user);
}
