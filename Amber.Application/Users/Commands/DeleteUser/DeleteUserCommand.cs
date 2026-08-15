using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : ICommand;
