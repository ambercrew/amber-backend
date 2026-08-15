using Amber.Application.Services;
using Amber.Domain.Users.Entities;

namespace Amber.Application.Users.Services;

public interface IUserEmailVerificationCodeSender : IApplicationService
{
    Task SendVerificationEmailAsync(User user);
}
